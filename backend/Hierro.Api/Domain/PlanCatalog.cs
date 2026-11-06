using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hierro.Api.Domain;

/// <summary>The training plans, loaded once from the embedded plans.json.</summary>
public sealed class PlanCatalog(IReadOnlyList<Plan> plans)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public IReadOnlyList<Plan> Plans { get; } = plans;

    public Plan? FindPlan(string planId) => Plans.FirstOrDefault(p => p.Id == planId);

    public WorkoutDay? FindDay(string planId, string dayId) =>
        FindPlan(planId)?.Days.FirstOrDefault(d => d.Id == dayId);

    public static PlanCatalog LoadEmbedded()
    {
        using var stream = typeof(PlanCatalog).Assembly.GetManifestResourceStream("Hierro.Api.plans.json")
            ?? throw new InvalidOperationException("Embedded resource Hierro.Api.plans.json is missing.");
        var plans = JsonSerializer.Deserialize<List<Plan>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("plans.json is empty.");
        return new PlanCatalog(plans);
    }
}
