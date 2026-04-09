using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Dtos
{
    public class CreateCourseDto
    {
        [Required]
        public string CourseName { get; set; } = null!;

        public int CreditHours { get; set; }

        public string? Description { get; set; }
    }

    public class CourseDto
    {
        public int Id { get; set; }

        public string CourseName { get; set; } = null!;

        public int CreditHours { get; set; }

        public string? Description { get; set; }
    }
}

