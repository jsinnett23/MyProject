using System;

namespace MyProject.Backend.Models
{


    public class Band
    {

        //These are the main variables in Band
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Genre { get; set; }
        public DateTime? DateTime { get; set; }
        public string? Stage { get; set; }

        //Constructor Null
        public Band()
        {
            Id = 0;
            Name = null;
            Genre = null;
            DateTime = null;
            Stage = null;
        }

        //Constructor
        public Band(int id, string? name, string? genre, DateTime dateTime, string? stage)
        {
            Id = id;
            Name = name;
            Genre = genre;
            DateTime = dateTime;
            Stage = stage;
        }

    }
}