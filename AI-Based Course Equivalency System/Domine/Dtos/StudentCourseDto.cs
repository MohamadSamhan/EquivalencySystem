using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Domine.Dtos
{
    public class CreateStudentCourseDto
    {

        [Required]
        public string CourseName { get; set; } = null!;

        public int CreditHours { get; set; }

        public string? Description { get; set; }

        // Student selects from predefined universities
        [Required]
        public int UniversityId { get; set; }

        // Optional file uploaded by the student (use portable representation instead of IFormFile)
        // When using the Web API layer you can accept IFormFile there and map into these fields.
        public string? FileName { get; set; }
        public byte[]? FileContent { get; set; }
    }

    public class StudentCourseDto
    {
        public int Id { get; set; }

        public string CourseName { get; set; } = null!;

        public int CreditHours { get; set; }

        public string? Description { get; set; }

        public int UniversityId { get; set; }

        // Convenience for the frontend
        public string? UniversityName { get; set; }

        // Info about uploaded file (if any)
        public string? UploadedFileName { get; set; }
        public string? UploadedFileHash { get; set; }
    }
}
