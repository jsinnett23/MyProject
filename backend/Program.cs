
using Microsoft.EntityFrameworkCore;
using MyProject.Backend.Data;
using MyProject.Backend.Models;
using MyProject.Backend.Dtos;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Allow the React dev server to call this API during development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddDbContext<MusicFestivalContext>(options =>
    options.UseSqlite("Data Source=musicfestival.db"));

var app = builder.Build();


// GET /api/bands with paging, filtering and sorting
app.MapGet("/api/bands", async (
    MusicFestivalContext db,
    string? genre,
    string? stage,
    string? sortBy,
    int page = 1,
    int pageSize = 20,
    string? dateFrom = null,
    string? dateTo = null) =>
{
    if (page < 1) page = 1;
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = db.Bands.AsQueryable();

    if (!string.IsNullOrWhiteSpace(genre))
    {
        var g = genre.Trim();
        query = query.Where(b => b.Genre != null && b.Genre.ToLower().Contains(g.ToLower()));
    }

    if (!string.IsNullOrWhiteSpace(stage))
    {
        var s = stage.Trim();
        query = query.Where(b => b.Stage != null && b.Stage.ToLower().Contains(s.ToLower()));
    }

    DateTime? df = null, dt = null;
    if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedFrom))
        df = parsedFrom;
    if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedTo))
        dt = parsedTo;

    if (df.HasValue)
        query = query.Where(b => b.DateTime.HasValue && b.DateTime >= df.Value);
    if (dt.HasValue)
        query = query.Where(b => b.DateTime.HasValue && b.DateTime <= dt.Value);

    // Sorting
    var desc = false;
    var key = (sortBy ?? "date").Trim();
    if (key.StartsWith("-")) { desc = true; key = key[1..].Trim(); }

    query = key.ToLower() switch
    {
        "name" => desc ? query.OrderByDescending(b => b.Name) : query.OrderBy(b => b.Name),
        "date" => desc ? query.OrderByDescending(b => b.DateTime) : query.OrderBy(b => b.DateTime),
        _ => query.OrderBy(b => b.Id),
    };

    var totalCount = await query.CountAsync();
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(b => new BandReadDto
        {
            Id = b.Id,
            Name = b.Name,
            Genre = b.Genre,
            DateTime = b.DateTime,
            Stage = b.Stage
        })
        .ToListAsync();

    var result = new
    {
        totalCount,
        page,
        pageSize,
        totalPages,
        items
    };

    return Results.Ok(result);
});

//This function post a new band to the database (inline validation)
app.MapPost("/api/bands", async (MyProject.Backend.Dtos.BandCreateDto dto, MusicFestivalContext db) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(dto.Name))
        errors["name"] = new[] { "Name is required." };
    else if (dto.Name.Length > 200)
        errors["name"] = new[] { "Name can't be longer than 200 characters." };

    if (!string.IsNullOrEmpty(dto.Genre) && dto.Genre.Length > 100)
        errors["genre"] = new[] { "Genre can't be longer than 100 characters." };

    if (!string.IsNullOrEmpty(dto.Stage) && dto.Stage.Length > 100)
        errors["stage"] = new[] { "Stage can't be longer than 100 characters." };

    if (errors.Any()) return Results.ValidationProblem(errors);

    var band = new Band
    {
        Name = dto.Name,
        Genre = dto.Genre,
        DateTime = dto.DateTime,
        Stage = dto.Stage
    };

    db.Bands.Add(band);
    await db.SaveChangesAsync();

    var read = new BandReadDto { Id = band.Id, Name = band.Name, Genre = band.Genre, DateTime = band.DateTime, Stage = band.Stage };
    return Results.Created($"/api/bands/{band.Id}", read);
});

// Get a single band by id
app.MapGet("/api/bands/{id}", async (int id, MusicFestivalContext db) =>
{
    var band = await db.Bands.FindAsync(id);
    return band is not null ? Results.Ok(band) : Results.NotFound();
});

// Update an existing band (inline validation)
app.MapPut("/api/bands/{id}", async (int id, MyProject.Backend.Dtos.BandUpdateDto dto, MusicFestivalContext db) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(dto.Name))
        errors["name"] = new[] { "Name is required." };
    else if (dto.Name.Length > 200)
        errors["name"] = new[] { "Name can't be longer than 200 characters." };

    if (!string.IsNullOrEmpty(dto.Genre) && dto.Genre.Length > 100)
        errors["genre"] = new[] { "Genre can't be longer than 100 characters." };

    if (!string.IsNullOrEmpty(dto.Stage) && dto.Stage.Length > 100)
        errors["stage"] = new[] { "Stage can't be longer than 100 characters." };

    if (errors.Any()) return Results.ValidationProblem(errors);

    var existing = await db.Bands.FindAsync(id);
    if (existing is null) return Results.NotFound();

    existing.Name = dto.Name;
    existing.Genre = dto.Genre;
    existing.DateTime = dto.DateTime;
    existing.Stage = dto.Stage;

    await db.SaveChangesAsync();

    var read = new BandReadDto { Id = existing.Id, Name = existing.Name, Genre = existing.Genre, DateTime = existing.DateTime, Stage = existing.Stage };
    return Results.Ok(read);
});

// Delete a band
app.MapDelete("/api/bands/{id}", async (int id, MusicFestivalContext db) =>
{
    var existing = await db.Bands.FindAsync(id);
    if (existing is null) return Results.NotFound();

    db.Bands.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Below is middleware for adding 500 error
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = builder.Environment.IsDevelopment() ? ex?.ToString() : "Internal server error",
            Instance = context.Request.Path
        };

        context.Response.StatusCode = pd.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(pd, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
    });
});

// Enable CORS for the configured dev origins
app.UseCors("LocalDev");

app.UseHttpsRedirection();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };


// app.MapGet("/weatherforecast", () =>
// {
//     var forecast = Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast");




// In Development, apply any pending migrations and seed sample data.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<MusicFestivalContext>();

        // Apply migrations (idempotent) so the DB schema matches the model.
        db.Database.Migrate();

        // Idempotent seed: will do nothing if data already exists.
        SeedData.EnsureSeedData(db);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Simple, idempotent seed helper included inline so it's easy to review.
public static class SeedData
{
    public static void EnsureSeedData(MusicFestivalContext context)
    {
        // Idempotency: if there are any bands, assume DB already seeded.
        if (context.Bands.Any())
        {
            return;
        }

        var sampleBands = new[]
        {
            new Band { Name = "The Rolling Bytes", Genre = "Rock", Stage = "Main", DateTime = new DateTime(2026, 6, 12, 20, 0, 0) },
            new Band { Name = "Synth Sunrise", Genre = "Electronic", Stage = "Electro", DateTime = new DateTime(2026, 6, 12, 22, 0, 0) },
            new Band { Name = "Folk & Loops", Genre = "Folk", Stage = "Acoustic", DateTime = new DateTime(2026, 6, 13, 18, 30, 0) },
        };

        context.Bands.AddRange(sampleBands);
        context.SaveChanges();
    }
}
