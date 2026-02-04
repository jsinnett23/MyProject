using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using MyProject.Backend.Data;
using Microsoft.Extensions.Options;

namespace MyProject.Api.IntegrationTest
{

    public class MyProjectWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"test-db-{Guid.NewGuid():N}.db"); //unique temp file for SQLite test DB
        //we need to override ConfigureWebhost to edit it before it builds
        protected override void ConfigureWebHost (IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, conf) =>
            {
                var dictionary = new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "test-secret-key-which-is-long-enough", //JWT secret for token service
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}" //this string points to temp SQLite file
                };
                conf.AddInMemoryCollection(dictionary);
            }); //configuring additional memory for test application

            builder.ConfigureServices(services => //configure test container
            {

                // find existing DbContextOptions and remove it
                var descriptor = services.SingleOrDefault(d  => d.ServiceType == typeof(DbContextOptions<MusicFestivalContext>));
                if (descriptor != null) services.Remove(descriptor); // this actually removes the existing registration file

                //register the app's DbContext to use SQlite with the temp DB file
                services.AddDbContext<MusicFestivalContext> (Options => Options.UseSqlite($"Data Source ={_dbPath}")); //this points to the temporary file created above

                //build a temporary provider to run migrations
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope()) //workign within this scope allows us to resolve the actual DbContext file
                {
                    var db = scope.ServiceProvider.GetRequiredService<MusicFestivalContext>();
                    db.Database.Migrate(); //apply the actual migrations
                }

            });

        }

        //we need to override dispose
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                if (File.Exists(_dbPath)) File.Delete(_dbPath); //delete the temp sql db
            }
            catch
            {
                
            }
        }   
    }
    
}