# WeightRecall

WeightRecall is a mobile application built with .NET MAUI designed to help you track your weightlifting progress. It allows you to define a weekly workout routine and log your performance (sets, reps, and weight) for each exercise, providing historical context to help you push your limits.

## Features

- **Weekly Routine Management**: Organize your exercises by day of the week.
- **Workout Logging**: Easily record sets, repetitions, and weight for your sessions.
- **Progress Tracking**: Visualize your strength gains with interactive progress charts.
- **Contextual Performance**: View your performance from the previous week directly while logging to ensure progressive overload.
- **Daily Reminders**: Optional local notifications to remind you of your scheduled exercises for the day.
- **Dark/Light Mode Support**: Adaptive UI that matches your system theme.

## Tech Stack

- **Framework**: [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/)
- **Database**: [SQLite](https://www.sqlite.org/) with [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net)
- **MVVM Pattern**: [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- **Charts**: [Microcharts](https://github.com/microcharts-dotnet/Microcharts)
- **Notifications**: [Plugin.LocalNotification](https://github.com/thudugala/Plugin.LocalNotification)
- **Logging**: [Serilog](https://serilog.net/)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 with the "Mobile development with .NET" workload.

### Installation

1. Go to the [Releases](https://github.com/LifeTrue1981815214/WeightRecall/releases) page on GitHub.
2. Download the latest `.apk` file.
3. Transfer the APK to your Android device and install it (ensure "Install from Unknown Sources" is enabled in your device settings).

### Development Setup

If you wish to build the project yourself:

1. Clone the repository:
   ```bash
   git clone https://github.com/LifeTrue1981815214/WeightRecall.git
   ```

2. Open `WeightRecall.slnx` or `WeightRecall.csproj` in Visual Studio.

3. Restore dependencies:
   ```bash
   dotnet restore
   ```

4. Build and run the application on your preferred platform (Android, iOS, or Windows).

## Project Structure

- **Models/**: Data entities (`WorkoutLog`, `RoutineItem`, etc.).
- **Data/**: Database context and initialization.
- **Repository/**: Data access layer using SQLite.
- **Services/**: Business logic, notifications, and date utilities.
- **ViewModels/**: Application state and command handling using MVVM.
- **Resources/**: Styles, fonts, and images.

## License

This project is licensed under the [LICENSE](LICENSE) file found in the root directory.

