using System;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Backend.Dtos
{
    public class BandUpdateDto
    {
        [Required]
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Genre { get; set; }

        public DateTime? DateTime { get; set; }

        [StringLength(100)]
        public string? Stage { get; set; }
    }
}
