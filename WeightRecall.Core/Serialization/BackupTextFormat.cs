using System.Globalization;
using System.Text;
using WeightRecall.Models;

namespace WeightRecall.Serialization;

/// <summary>
/// Reads and writes the plain-text backup format: two named sections, each a CSV table.
/// </summary>
/// <remarks>
/// Both halves of the format live here on purpose. They have to agree on quoting and on the
/// section names, and when they were split across a writer and a reader that agreement was
/// only ever checked by hand.
/// </remarks>
public static class BackupTextFormat
{
    // Section names predate the renames to PlannedExercise and ExerciseLog and are pinned for
    // the same reason as the JSON keys; see BackupData. The writer and the reader must use
    // these same two constants, or an exported file will not read back.
    private const string PlannedExerciseSection = "RoutineItems";
    private const string ExerciseLogSection = "WorkoutLogs";

    // The trailing column here is the pinned pre-rename name of PlannedExerciseBackup.Position.
    private const string PlannedExerciseHeader = "ExerciseName,DayOfWeek,Order";
    private const string ExerciseLogHeader = "Date,ExerciseName,Sets,Reps,Weight";

    /// <summary>
    /// Renders a backup as text.
    /// </summary>
    public static string Write(BackupData data)
    {
        StringBuilder sb = new();

        sb.AppendLine($"[{PlannedExerciseSection}]");
        sb.AppendLine(PlannedExerciseHeader);
        foreach (PlannedExerciseBackup e in data.PlannedExercises)
        {
            sb.AppendLine($"{CsvEscape(e.ExerciseName)},{e.DayOfWeek},{e.Position}");
        }

        sb.AppendLine();
        sb.AppendLine($"[{ExerciseLogSection}]");
        sb.AppendLine(ExerciseLogHeader);
        foreach (ExerciseLogBackup w in data.ExerciseLogs)
        {
            sb.AppendLine(
                FormattableString.Invariant(
                    $"{w.Date:O},{CsvEscape(w.ExerciseName)},{w.Sets},{w.Reps},{w.Weight}"
                )
            );
        }

        return sb.ToString();
    }

    /// <summary>
    /// Reads a backup from text.
    /// </summary>
    /// <remarks>
    /// Lines that do not carry enough columns are skipped. Text that contains no recognizable
    /// section at all is not an error: it parses to an empty backup.
    /// TODO: that silence is dangerous, because restoring an empty backup erases everything.
    /// See <see cref="BackupData.IsEmpty"/>.
    /// </remarks>
    /// <exception cref="FormatException">A value in a data row could not be parsed.</exception>
    public static BackupData Parse(string content)
    {
        BackupData data = new();
        string? section = null;
        bool headerSkipped = false;

        foreach (string rawLine in content.Split('\n'))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line == $"[{PlannedExerciseSection}]")
            {
                section = PlannedExerciseSection;
                headerSkipped = false;
                continue;
            }
            if (line == $"[{ExerciseLogSection}]")
            {
                section = ExerciseLogSection;
                headerSkipped = false;
                continue;
            }

            // The first line after a section marker is its column header, and is discarded.
            if (!headerSkipped)
            {
                headerSkipped = true;
                continue;
            }

            string[] parts = SplitCsvLine(line);

            if (section == PlannedExerciseSection && parts.Length >= 3)
            {
                data.PlannedExercises.Add(
                    new PlannedExerciseBackup
                    {
                        ExerciseName = parts[0],
                        DayOfWeek = Enum.Parse<DayOfWeek>(parts[1]),
                        Position = int.Parse(parts[2], CultureInfo.InvariantCulture),
                    }
                );
            }
            else if (section == ExerciseLogSection && parts.Length >= 5)
            {
                data.ExerciseLogs.Add(
                    new ExerciseLogBackup
                    {
                        Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
                        ExerciseName = parts[1],
                        Sets = int.Parse(parts[2], CultureInfo.InvariantCulture),
                        Reps = int.Parse(parts[3], CultureInfo.InvariantCulture),
                        Weight = double.Parse(parts[4], CultureInfo.InvariantCulture),
                    }
                );
            }
        }

        return data;
    }

    /// <summary>
    /// Splits one CSV line into its fields, ignoring commas inside a quoted field.
    /// </summary>
    /// <remarks>
    /// TODO: this does not undo the doubled quotes that <see cref="CsvEscape"/> writes. Every
    /// quote character is treated as a delimiter and dropped, so a name containing one does not
    /// survive a round trip, and a comma after it lands outside the quoted run and splits the
    /// field in the wrong place.
    /// </remarks>
    private static string[] SplitCsvLine(string line)
    {
        List<string> result = [];
        bool inQuotes = false;
        StringBuilder current = new();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                _ = current.Append(c);
            }
        }

        result.Add(current.ToString());
        return [.. result];
    }

    /// <summary>
    /// Wraps a field in quotes if it contains anything that would break the row apart.
    /// </summary>
    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
