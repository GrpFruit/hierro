using System.Text.Json;
using Hierro.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hierro.Api.Data;

/// <summary>A logged workout. One per day of the plan per date; the id is "{date}_{dayId}".</summary>
public sealed class WorkoutSession
{
    public required string Id { get; set; }
    public required DateOnly Date { get; set; }
    public required string PlanId { get; set; }
    public required string DayId { get; set; }
    public required string DayName { get; set; }
    public required DateTime SavedAtUtc { get; set; }
    public List<SessionEntry> Entries { get; set; } = [];

    public static string MakeId(DateOnly date, string dayId) => $"{date:yyyy-MM-dd}_{dayId}";
}

public sealed record SessionEntry(string Name, List<SetLog> Sets, string Note);

public sealed class HierroDb(DbContextOptions<HierroDb> options) : DbContext(options)
{
    public DbSet<WorkoutSession> Sessions => Set<WorkoutSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var session = modelBuilder.Entity<WorkoutSession>();
        session.HasKey(s => s.Id);
        session.Property(s => s.Id).HasMaxLength(64);
        session.HasIndex(s => s.Date);
        // Entries are always read and written whole, so they live in one JSON column.
        session.Property(s => s.Entries)
            .HasColumnName("EntriesJson")
            .HasConversion(
                v => EntriesJson.Write(v),
                v => EntriesJson.Read(v),
                new ValueComparer<List<SessionEntry>>(
                    (a, b) => EntriesJson.Write(a) == EntriesJson.Write(b),
                    v => EntriesJson.Write(v).GetHashCode(),
                    v => EntriesJson.Read(EntriesJson.Write(v))));
    }
}

internal static class EntriesJson
{
    public static string Write(List<SessionEntry>? entries) =>
        JsonSerializer.Serialize(entries ?? [], PlanCatalog.JsonOptions);

    public static List<SessionEntry> Read(string json) =>
        JsonSerializer.Deserialize<List<SessionEntry>>(json, PlanCatalog.JsonOptions) ?? [];
}
