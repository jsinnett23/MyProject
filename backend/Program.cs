
using Microsoft.EntityFrameworkCore;
using MyProject.Backend.Data;
using MyProject.Backend.Models;
using MyProject.Backend.Dtos;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<MusicFestivalContext>(options =>
    options.UseSqlite("Data Source=musicfestival.db"));

var app = builder.Build();

// Use global exception handling middleware
app.UseMiddleware<MyProject.Backend.Middleware.ExceptionMiddleware>();


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

//This function post a new band to the database
app.MapPost("/api/bands", async (Band band,
MusicFestivalContext db) =>
{
    db.Bands.Add(band);
    await db.SaveChangesAsync(); return
    Results.Created($"/api/bands/{band.Id}", band);
});

// Get a single band by id
app.MapGet("/api/bands/{id}", async (int id, MusicFestivalContext db) =>
{
    var band = await db.Bands.FindAsync(id);
    return band is not null ? Results.Ok(band) : Results.NotFound();
});

// Update an existing band
app.MapPut("/api/bands/{id}", async (int id, Band updatedBand, MusicFestivalContext db) =>
{
    var existing = await db.Bands.FindAsync(id);
    if (existing is null) return Results.NotFound();

    // Update allowed fields
    existing.Name = updatedBand.Name;
    existing.Genre = updatedBand.Genre;
    existing.DateTime = updatedBand.DateTime;
    existing.Stage = updatedBand.Stage;

    await db.SaveChangesAsync();
    return Results.Ok(existing);
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




app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
