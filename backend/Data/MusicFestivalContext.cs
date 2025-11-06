using Microsoft.EntityFrameworkCore;
using MyProject.Backend.Models;
using Microsoft.EntityFrameworkCore.Design;


//Namespace makes naming conventions easiser
namespace MyProject.Backend.Data
{
    //This class will be a subclass of DbContext 
    // (impoorted with the Entitiy Framework Above)
    // This DbContext file will directly communicate with the SQL file.
    public class MusicFestivalContext : DbContext
    {
        //Constructor
        public MusicFestivalContext(DbContextOptions<MusicFestivalContext> options)
            //this is contstructor of base/parent class 
            //same as DbContext db = new DbContext(options);
            : base(options) { }

        public DbSet<Band> Bands { get; set; } = null!;

    }

    //EF was struggling to find my context file so I added the code below
    //This gives the factory EF CLI, an explicit way to create DbContext
    public class MusicFestivalContextFactory : IDesignTimeDbContextFactory<MusicFestivalContext>
    {
        public MusicFestivalContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MusicFestivalContext>();
            optionsBuilder.UseSqlite("Data Source=musicfestival.db");
            return new MusicFestivalContext(optionsBuilder.Options);
        }
    }
}


