using System.Text.Json;
using System.Text.Json.Serialization;
using Hierro.Api.Data;
using Hierro.Api.Domain;
using Hierro.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddSingleton(PlanCatalog.LoadEmbedded());
builder.Services.AddDbContext<HierroDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Hierro") ?? "Data Source=hierro.db"));

// The Next.js dev server proxies /api to this app, so the browser never calls it cross-origin.
// Origins listed under "Cors:Origins" are allowed for anything that calls the API directly.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Placeholder schema setup: creates the SQLite file on first run.
// Switch to EF Core migrations before the schema starts changing.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<HierroDb>().Database.EnsureCreated();
}

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapWorkoutEndpoints();

app.Run();

public partial class Program;
