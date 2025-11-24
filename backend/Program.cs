
using Microsoft.EntityFrameworkCore;
using MyProject.Backend.Data;
using MyProject.Backend.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<MusicFestivalContext>(options =>
    options.UseSqlite("Data Source=musicfestival.db"));

var app = builder.Build();


//This function list the bands playing
app.MapGet("/api/bands", async
(MusicFestivalContext db) => await
db.Bands.ToListAsync());

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
