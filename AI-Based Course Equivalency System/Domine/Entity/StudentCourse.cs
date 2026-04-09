using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domine.Entity
{
    public class StudentCourse
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Student))]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(250)]
        public string CourseName { get; set; } = null!;

        public int CreditHours { get; set; }

        public string? Description { get; set; }

        // Replace free-text UniversityName with a FK to predefined universities
        [Required]
        [ForeignKey(nameof(University))]
        public int UniversityId { get; set; }

        public University? University { get; set; }

        public User? Student { get; set; }

        // Added fields to store uploaded file metadata
        public string? UploadedFilePath { get; set; }
        public string? UploadedFileName { get; set; }
        public string? UploadedFileHash { get; set; }
    }
}
