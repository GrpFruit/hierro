using Hierro.Api.Data;
using Hierro.Api.Domain;

namespace Hierro.Api.Endpoints;

/// <summary>Everything the session screen needs for one day on one date.</summary>
public sealed record SheetResponse(
    string PlanId,
    string PlanName,
    string DayId,
    string DayName,
    string Focus,
    DateOnly Date,
    string SessionId,
    DateTime? SavedAtUtc,
    IReadOnlyList<SheetExercise> Exercises);

public sealed record SheetExercise(
    Exercise Exercise,
    CoachAdvice Coach,
    PastPerformance? Last,
    IReadOnlyList<double>? PlatesPerSide,
    IReadOnlyList<string>? WarmUps,
    SessionEntry? Saved);

public sealed record SessionSummary(
    string Id,
    DateOnly Date,
    string PlanId,
    string DayId,
    string DayName,
    DateTime SavedAtUtc,
    int SetCount,
    MainLiftTop? MainLift);

public sealed record MainLiftTop(string Label, double TopWeight);

public sealed record LiftProgress(string Name, string Label, IReadOnlyList<LiftPoint> Points);

public sealed record LiftPoint(DateOnly Date, double Weight, int Reps);

public sealed record SaveSessionRequest(string PlanId, List<SessionEntryInput>? Entries);

public sealed record SessionEntryInput(string Name, List<SetLog>? Sets, string? Note);

public static class SessionMapping
{
    public static SessionSummary ToSummary(this WorkoutSession s)
    {
        var main = s.Entries.FirstOrDefault(e => e.Sets.Count > 0 && MainLifts.LabelFor(e.Name) is not null);
        return new SessionSummary(
            s.Id, s.Date, s.PlanId, s.DayId, s.DayName, s.SavedAtUtc,
            s.Entries.Sum(e => e.Sets.Count),
            main is null ? null : new MainLiftTop(MainLifts.LabelFor(main.Name)!, Coach.TopWeight(main.Sets)));
    }
}
