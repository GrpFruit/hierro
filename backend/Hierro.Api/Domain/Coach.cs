using static System.FormattableString;

namespace Hierro.Api.Domain;

public sealed record SetLog(double W, int R);

/// <summary>One earlier session's sets for a single exercise.</summary>
public sealed record PastPerformance(DateOnly Date, IReadOnlyList<SetLog> Sets);

public sealed record CoachAdvice(string Tone, string Label, string Text, double? Weight);

/// <summary>
/// Progression rules: linear progression on the main lifts (with a 10% reset after two
/// missed sessions at the same weight), double progression on helper lifts, and harder
/// variations for bodyweight moves.
/// </summary>
public static class Coach
{
    public const double Bar = 45;
    private static readonly double[] PlateSizes = [45, 35, 25, 10, 5, 2.5];

    /// <summary>Rounds to the nearest 5 lb, halves going up (matches the original tracker).</summary>
    public static double RoundTo5(double x) => Math.Round(x / 5, MidpointRounding.AwayFromZero) * 5;

    public static double TopWeight(IReadOnlyList<SetLog> sets) => sets.Count == 0 ? 0 : Math.Max(0, sets.Max(s => s.W));

    public static bool HitAll(Exercise ex, IReadOnlyList<SetLog> sets) =>
        sets.Count >= ex.Sets && sets.All(s => s.R >= ex.RepRange.Hi);

    /// <param name="history">Earlier sessions for this exercise, newest first.</param>
    public static CoachAdvice Advise(Exercise ex, IReadOnlyList<PastPerformance> history)
    {
        var (lo, hi) = ex.RepRange;
        var bodyweight = ex.Kind == ExerciseKind.Bodyweight;

        if (history.Count == 0)
        {
            if (bodyweight) return new("first", "Start", Invariant($"First time: aim for {lo} clean reps per set."), null);
            if (ex.Main) return new("first", "Start", "First time: pick a weight you could lift about 8 times, then stop at 5.", null);
            return new("first", "Start", Invariant($"First time: pick a weight that leaves about 2 reps in the tank at {hi}."), null);
        }

        var last = history[0];
        var top = TopWeight(last.Sets);
        var done = HitAll(ex, last.Sets);

        if (bodyweight)
        {
            return done
                ? new("up", "Level up", Invariant($"You hit {hi} on every set. Make it harder: {ex.ProgressionTip ?? "slow the lowering down"}."), null)
                : new("stay", "Hold", "Beat last time by one rep on any set.", null);
        }

        if (top <= 0) return new("stay", "Hold", "Log the weight you use and the tracker will tell you when to go up.", null);

        if (ex.Main && history.Count >= 2 && !done)
        {
            var prev = history[1];
            if (TopWeight(prev.Sets) == top && !HitAll(ex, prev.Sets))
            {
                var reset = Math.Max(Bar, RoundTo5(top * 0.9));
                return new("drop", "Reset", Invariant($"Missed reps two sessions in a row at {top} lb. Drop to {reset} lb and build back up."), reset);
            }
        }

        if (done)
        {
            var next = top + ex.Increment;
            var text = ex.Unit == ExerciseUnit.Meters ? Invariant($"Every carry done. Go heavier: {next} lb in each hand.")
                : ex.Kind == ExerciseKind.Dumbbell ? Invariant($"All sets hit {hi}. Go up a dumbbell: {next} lb each.")
                : Invariant($"All sets hit {hi}. Add weight: {next} lb.");
            return new("up", "Add weight", text, next);
        }

        return new("stay", "Hold", Invariant($"Stay at {top} lb and beat last time's reps."), top);
    }

    /// <summary>Plates per side for a barbell load; empty for the bare bar, null if lighter than the bar.</summary>
    public static IReadOnlyList<double>? PlatesPerSide(double total)
    {
        var perSide = (total - Bar) / 2;
        if (perSide < 0) return null;
        var plates = new List<double>();
        foreach (var p in PlateSizes)
        {
            while (perSide >= p - 0.001)
            {
                plates.Add(p);
                perSide -= p;
            }
        }
        return plates;
    }

    /// <summary>Ramp-up sets for a main lift: bar × 10 → 50% × 5 → 70% × 3 → 85% × 1–2.</summary>
    public static IReadOnlyList<string>? WarmUps(double workWeight)
    {
        if (workWeight <= Bar + 10) return null;
        (double Weight, string Reps)[] steps =
        [
            (Bar, "10"),
            (RoundTo5(workWeight * 0.5), "5"),
            (RoundTo5(workWeight * 0.7), "3"),
            (RoundTo5(workWeight * 0.85), "1–2"),
        ];
        var result = new List<string>();
        double prev = 0;
        foreach (var (w, reps) in steps)
        {
            var v = Math.Max(Bar, w);
            if (v > prev && v < workWeight)
            {
                result.Add(Invariant($"{v}×{reps}"));
                prev = v;
            }
        }
        return result;
    }
}
