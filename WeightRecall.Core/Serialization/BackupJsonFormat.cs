using System.Text.Json;
using System.Text.Json.Serialization;
using WeightRecall.Models;

namespace WeightRecall.Serialization;

/// <summary>
/// Reads and writes the JSON backup format.
/// </summary>
public static class BackupJsonFormat
{
    /// <summary>
    /// The options both directions share. Enums are written as names rather than numbers, so a
    /// backup stays readable and stays valid if the underlying enum values are ever reordered.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Renders a backup as JSON text.
    /// </summary>
    public static string Write(BackupData data) => JsonSerializer.Serialize(data, Options);

    /// <summary>
    /// Reads a backup from JSON text.
    /// </summary>
    /// <returns>The parsed backup, or <c>null</c> if the file held only a JSON null.</returns>
    /// <exception cref="JsonException">The text is not valid JSON.</exception>
    public static BackupData? Read(string json) =>
        JsonSerializer.Deserialize<BackupData>(json, Options);
}
