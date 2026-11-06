using System.Globalization;
using Hierro.Api.Data;
using Hierro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hierro.Api.Endpoints;

public static class WorkoutEndpoints
{
    private const int MaxNoteLength = 300;
    private const int MaxReps = 1000;
    private const double MaxWeight = 2000;

    public static void MapWorkoutEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/plans", (PlanCatalog catalog) => catalog.Plans)
            .WithName("GetPlans");

        api.MapGet("/plans/{planId}/days/{dayId}/sheet", GetSheet)
            .WithName("GetSheet")
            .WithSummary("A day's exercises with coaching based on sessions before the given date.");

        api.MapGet("/sessions", async (HierroDb db, CancellationToken ct) =>
            (await NewestFirst(db.Sessions).ToListAsync(ct)).Select(s => s.ToSummary()))
            .WithName("GetSessions");

        api.MapPut("/sessions/{date}/{dayId}", SaveSession)
            .WithName("SaveSession");

        api.MapDelete("/sessions/{id}", async (string id, HierroDb db, CancellationToken ct) =>
            await db.Sessions.Where(s => s.Id == id).ExecuteDeleteAsync(ct) > 0
                ? Results.NoContent()
                : Results.NotFound())
            .WithName("DeleteSession");

        api.MapGet("/progress", GetProgress)
            .WithName("GetProgress")
            .WithSummary("Heaviest set per session for each main lift, oldest first.");
    }

    private static IQueryable<WorkoutSession> NewestFirst(IQueryable<WorkoutSession> q) =>
        q.OrderByDescending(s => s.Date).ThenByDescending(s => s.SavedAtUtc);

    private static async Task<IResult> GetSheet(
        string planId, string dayId, string date, PlanCatalog catalog, HierroDb db, CancellationToken ct)
    {
        if (!TryParseDate(date, out var day)) return Results.BadRequest("date must be YYYY-MM-DD.");
        var plan = catalog.FindPlan(planId);
        var workout = catalog.FindDay(planId, dayId);
        if (plan is null || workout is null) return Results.NotFound();

        var sessionId = WorkoutSession.MakeId(day, dayId);
        var saved = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        var earlier = await NewestFirst(db.Sessions.AsNoTracking().Where(s => s.Date < day)).ToListAsync(ct);

        var exercises = workout.Exercises.Select(ex =>
        {
            var history = earlier
                .Select(s => (s.Date, Entry: s.Entries.FirstOrDefault(e => e.Name == ex.Name)))
                .Where(x => x.Entry is { Sets.Count: > 0 })
                .Select(x => new PastPerformance(x.Date, x.Entry!.Sets))
                .ToList();
            var advice = Coach.Advise(ex, history);
            var plates = ex.Kind == ExerciseKind.Barbell && advice.Weight is { } w ? Coach.PlatesPerSide(w) : null;
            var warmUps = ex.Main && advice.Weight is { } mw ? Coach.WarmUps(mw) : null;
            return new SheetExercise(ex, advice, history.FirstOrDefault(), plates, warmUps,
                saved?.Entries.FirstOrDefault(e => e.Name == ex.Name));
        }).ToList();

        return Results.Ok(new SheetResponse(plan.Id, plan.Name, workout.Id, workout.Name, workout.Focus,
            day, sessionId, saved?.SavedAtUtc, exercises));
    }

    private static async Task<IResult> SaveSession(
        string date, string dayId, SaveSessionRequest body, PlanCatalog catalog, HierroDb db, CancellationToken ct)
    {
        if (!TryParseDate(date, out var day)) return Results.BadRequest("date must be YYYY-MM-DD.");
        var workout = catalog.FindDay(body.PlanId, dayId);
        if (workout is null) return Results.NotFound();

        var errors = new Dictionary<string, string[]>();
        var entries = new List<SessionEntry>();
        foreach (var (input, i) in (body.Entries ?? []).Select((e, i) => (e, i)))
        {
            var ex = workout.Exercises.FirstOrDefault(e => e.Name == input.Name);
            if (ex is null) { errors[$"entries[{i}].name"] = [$"{input.Name} is not part of {workout.Name}."]; continue; }

            var sets = input.Sets ?? [];
            if (sets.Count > ex.Sets) errors[$"entries[{i}].sets"] = [$"{ex.Name} has {ex.Sets} sets."];
            if (sets.Any(s => s.R is <= 0 or > MaxReps)) errors[$"entries[{i}].sets.r"] = [$"Reps must be between 1 and {MaxReps}."];
            if (sets.Any(s => s.W is < 0 or > MaxWeight || double.IsNaN(s.W))) errors[$"entries[{i}].sets.w"] = [$"Weight must be between 0 and {MaxWeight} lb."];

            var note = (input.Note ?? "").Trim();
            if (note.Length > MaxNoteLength) errors[$"entries[{i}].note"] = [$"Notes are limited to {MaxNoteLength} characters."];

            if (sets.Count == 0 && note.Length == 0) continue;
            var cleanSets = ex.Kind == ExerciseKind.Bodyweight ? sets.Select(s => s with { W = 0 }).ToList() : sets;
            entries.Add(new SessionEntry(ex.Name, cleanSets, note));
        }
        if (entries.GroupBy(e => e.Name).Any(g => g.Count() > 1)) errors["entries"] = ["Each exercise can appear once."];
        if (!entries.Any(e => e.Sets.Count > 0)) errors["entries"] = ["Enter reps for at least one set."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        var id = WorkoutSession.MakeId(day, dayId);
        var session = await db.Sessions.FindAsync([id], ct);
        if (session is null)
        {
            session = new WorkoutSession
            {
                Id = id, Date = day, PlanId = body.PlanId, DayId = dayId, DayName = workout.Name, SavedAtUtc = DateTime.UtcNow,
            };
            db.Sessions.Add(session);
        }
        session.PlanId = body.PlanId;
        session.DayName = workout.Name;
        session.SavedAtUtc = DateTime.UtcNow;
        session.Entries = entries;
        await db.SaveChangesAsync(ct);
        return Results.Ok(session.ToSummary());
    }

    private static async Task<IEnumerable<LiftProgress>> GetProgress(HierroDb db, CancellationToken ct)
    {
        var sessions = await db.Sessions.AsNoTracking()
            .OrderBy(s => s.Date).ThenBy(s => s.SavedAtUtc).ToListAsync(ct);
        return MainLifts.All.Select(m => new LiftProgress(m.Name, m.Label, sessions
            .Select(s => (s.Date, Entry: s.Entries.FirstOrDefault(e => e.Name == m.Name)))
            .Where(x => x.Entry is { Sets.Count: > 0 } && Coach.TopWeight(x.Entry.Sets) > 0)
            .Select(x =>
            {
                var top = Coach.TopWeight(x.Entry!.Sets);
                return new LiftPoint(x.Date, top, x.Entry.Sets.Where(s => s.W == top).Max(s => s.R));
            })
            .ToList()));
    }

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
