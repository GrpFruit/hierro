using Hierro.Api.Domain;

namespace Hierro.Api.Tests;

public class CoachTests
{
    private static readonly PlanCatalog Catalog = PlanCatalog.LoadEmbedded();

    private static Exercise Ex(string name) =>
        Catalog.Plans.SelectMany(p => p.Days).SelectMany(d => d.Exercises).First(e => e.Name == name);

    private static PastPerformance Session(string date, params (double W, int R)[] sets) =>
        new(DateOnly.Parse(date), sets.Select(s => new SetLog(s.W, s.R)).ToList());

    [Fact]
    public void Catalog_loads_both_plans_with_explicit_kinds()
    {
        Assert.Equal(["3day", "4day"], Catalog.Plans.Select(p => p.Id));
        Assert.Equal(ExerciseKind.Barbell, Ex("Barbell Bench Press").Kind);
        Assert.Equal(ExerciseKind.EzBar, Ex("EZ Bar Skull Crusher").Kind);
        Assert.Equal(ExerciseKind.Dumbbell, Ex("Double Dumbbell Hammer Curl").Kind);
        Assert.Equal(ExerciseKind.Bodyweight, Ex("Barbell Inverted Row").Kind);
        Assert.Equal(ExerciseUnit.Meters, Ex("Double Dumbbell Suitcase Carry").Unit);
    }

    [Fact]
    public void First_session_main_lift_suggests_an_eight_rep_weight()
    {
        var advice = Coach.Advise(Ex("Barbell Bench Press"), []);
        Assert.Equal("first", advice.Tone);
        Assert.Null(advice.Weight);
        Assert.Contains("about 8 times", advice.Text);
    }

    [Fact]
    public void All_sets_hit_on_squat_adds_ten_pounds()
    {
        var squat = Ex("Barbell High Bar Back Squat");
        var advice = Coach.Advise(squat, [Session("2026-11-02", (135, 5), (135, 5), (135, 5), (135, 5))]);
        Assert.Equal(("up", 145.0), (advice.Tone, advice.Weight));
    }

    [Fact]
    public void All_sets_hit_on_dumbbell_helper_goes_up_a_dumbbell()
    {
        var advice = Coach.Advise(Ex("Double Dumbbell Hammer Curl"), [Session("2026-11-02", (25, 12), (25, 12))]);
        Assert.Equal(30.0, advice.Weight);
        Assert.Equal("All sets hit 12. Go up a dumbbell: 30 lb each.", advice.Text);
    }

    [Fact]
    public void Missing_reps_twice_at_the_same_weight_resets_by_ten_percent()
    {
        var bench = Ex("Barbell Bench Press");
        var advice = Coach.Advise(bench,
        [
            Session("2026-11-04", (135, 5), (135, 4), (135, 3), (135, 3)),
            Session("2026-11-02", (135, 5), (135, 5), (135, 4), (135, 3)),
        ]);
        Assert.Equal(("drop", 120.0), (advice.Tone, advice.Weight)); // 121.5 rounds to 120
    }

    [Fact]
    public void Missing_reps_once_holds_the_weight()
    {
        var advice = Coach.Advise(Ex("Barbell Bench Press"), [Session("2026-11-04", (135, 5), (135, 4), (135, 5), (135, 5))]);
        Assert.Equal(("stay", 135.0), (advice.Tone, advice.Weight));
    }

    [Fact]
    public void Bodyweight_move_levels_up_with_its_own_tip()
    {
        var row = Ex("Barbell Inverted Row");
        var advice = Coach.Advise(row, [Session("2026-11-03", (0, 12), (0, 12), (0, 12))]);
        Assert.Equal("up", advice.Tone);
        Assert.EndsWith("set the bar one notch lower.", advice.Text);
    }

    [Fact]
    public void Fractional_weights_format_with_a_dot()
    {
        var advice = Coach.Advise(Ex("Barbell Overhead Press"), [Session("2026-11-03", (72.5, 6), (72.5, 6), (72.5, 5))]);
        Assert.Equal("Stay at 72.5 lb and beat last time's reps.", advice.Text);
    }

    [Theory]
    [InlineData(45, new double[0])]
    [InlineData(135, new double[] { 45 })]
    [InlineData(185, new double[] { 45, 25 })]
    [InlineData(230, new double[] { 45, 45, 2.5 })]
    public void Plates_per_side(double total, double[] expected) =>
        Assert.Equal(expected, Coach.PlatesPerSide(total));

    [Fact]
    public void Lighter_than_the_bar_has_no_plate_breakdown() => Assert.Null(Coach.PlatesPerSide(40));

    [Fact]
    public void Warm_ups_round_halves_up_like_the_original()
    {
        // 50% of 225 is 112.5 → 115, not banker's-rounded 110.
        Assert.Equal(["45×10", "115×5", "160×3", "190×1–2"], Coach.WarmUps(225));
        Assert.Null(Coach.WarmUps(55));
    }
}
