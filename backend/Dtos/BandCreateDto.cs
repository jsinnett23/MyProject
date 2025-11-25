using System;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Backend.Dtos
{
    public class BandCreateDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name can't be longer than 200 characters.")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Genre can't be longer than 100 characters.")]
        public string? Genre { get; set; }

        // optional scheduled time
        public DateTime? DateTime { get; set; }

        [StringLength(100, ErrorMessage = "Stage can't be longer than 100 characters.")]
        public string? Stage { get; set; }
    }
}
