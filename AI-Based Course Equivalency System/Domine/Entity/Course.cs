using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Entity
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string CourseName { get; set; } = null!;

        public int CreditHours { get; set; }

        public string? Description { get; set; }

        // New: optional server path for the course reference PDF (relative to ContentRootPath)
        public string? ReferenceFilePath { get; set; }
    }
}
