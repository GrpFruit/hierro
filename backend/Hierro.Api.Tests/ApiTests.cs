using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Hierro.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hierro-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Hierro", $"Data Source={_dbPath}");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
    }
}

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static object Bench(params (double w, int r)[] sets) => new
    {
        planId = "3day",
        entries = new[] { new { name = "Barbell Bench Press", sets = sets.Select(s => new { s.w, s.r }), note = "" } },
    };

    [Fact]
    public async Task Saved_session_drives_the_next_sessions_coaching()
    {
        var put = await _client.PutAsJsonAsync("/api/sessions/2026-11-02/push", Bench((135, 5), (135, 5), (135, 5), (135, 5)));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var sheet = await _client.GetFromJsonAsync<JsonElement>("/api/plans/3day/days/push/sheet?date=2026-11-04");
        var bench = sheet.GetProperty("exercises")[0];
        Assert.Equal("barbell", bench.GetProperty("exercise").GetProperty("kind").GetString());
        Assert.Equal("up", bench.GetProperty("coach").GetProperty("tone").GetString());
        Assert.Equal(140, bench.GetProperty("coach").GetProperty("weight").GetDouble());
        Assert.Equal([45.0, 2.5], bench.GetProperty("platesPerSide").EnumerateArray().Select(p => p.GetDouble()));

        var progress = await _client.GetFromJsonAsync<JsonElement>("/api/progress");
        var benchPoints = progress[0].GetProperty("points");
        Assert.Equal(135, benchPoints[0].GetProperty("weight").GetDouble());

        var delete = await _client.DeleteAsync("/api/sessions/2026-11-02_push");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Exercises_from_another_day_are_rejected()
    {
        var body = new { planId = "3day", entries = new[] { new { name = "Barbell Hip Thrust", sets = new[] { new { w = 95, r = 8 } }, note = "" } } };
        var res = await _client.PutAsJsonAsync("/api/sessions/2026-11-02/push", body);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task A_session_needs_at_least_one_set() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync("/api/sessions/2026-11-02/push", Bench())).StatusCode);

    [Fact]
    public async Task Unknown_day_is_not_found() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/plans/3day/days/nope/sheet?date=2026-11-02")).StatusCode);
}
