namespace Hierro.Api.Domain;

public enum ExerciseKind { Barbell, EzBar, Dumbbell, Bodyweight }

public enum ExerciseUnit { Reps, Meters }

public sealed record Plan(
    string Id,
    string Name,
    string Subtitle,
    string Schedule,
    IReadOnlyList<PlanHighlight> Highlights,
    IReadOnlyList<WorkoutDay> Days);

public sealed record PlanHighlight(string Title, string Detail);

public sealed record WorkoutDay(string Id, string Name, string Focus, IReadOnlyList<Exercise> Exercises);

public sealed record Exercise(
    string Name,
    ExerciseKind Kind,
    int Sets,
    string Reps,
    string Rest,
    ExerciseUnit Unit,
    bool Main,
    bool PerSide,
    string? Note,
    string? ProgressionTip,
    string Target,
    string Equipment,
    string Video)
{
    /// <summary>Lower and upper bound of the rep target, e.g. "8-10" → (8, 10), "5" → (5, 5).</summary>
    public (int Lo, int Hi) RepRange
    {
        get
        {
            var parts = Reps.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var lo = int.Parse(parts[0]);
            return (lo, parts.Length > 1 ? int.Parse(parts[1]) : lo);
        }
    }

    /// <summary>How much weight to add when every set hits the top of the range.</summary>
    public double Increment =>
        Kind == ExerciseKind.Barbell && (Name.Contains("Squat") || Name.Contains("Deadlift")) ? 10 : 5;
}

/// <summary>The four barbell lifts charted on the progress page.</summary>
public static class MainLifts
{
    public static readonly IReadOnlyList<(string Name, string Label)> All =
    [
        ("Barbell Bench Press", "Bench Press"),
        ("Barbell High Bar Back Squat", "Back Squat"),
        ("Barbell Conventional Deadlift", "Deadlift"),
        ("Barbell Overhead Press", "Overhead Press"),
    ];

    public static string? LabelFor(string exerciseName) =>
        All.FirstOrDefault(m => m.Name == exerciseName).Label;
}
