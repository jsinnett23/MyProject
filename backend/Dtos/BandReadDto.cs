using System;

namespace MyProject.Backend.Dtos
{
    public class BandReadDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Genre { get; set; }
        public DateTime? DateTime { get; set; }
        public string? Stage { get; set; }
    }
}
