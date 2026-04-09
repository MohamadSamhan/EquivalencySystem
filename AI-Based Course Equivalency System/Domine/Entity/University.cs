using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domine.Entity
{
    public class University
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = null!;

        // Optional future field
        [MaxLength(100)]
        public string? Country { get; set; }

        public ICollection<StudentCourse>? StudentCourses { get; set; }
    }
}

