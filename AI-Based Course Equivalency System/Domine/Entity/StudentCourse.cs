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

        /// <summary>Provider / training center name (e.g. Coursera, Udemy, etc.)</summary>
        [MaxLength(300)]
        public string? ProviderName { get; set; }

        public int CreditHours { get; set; }

        /// <summary>Actual training hours on the certificate</summary>
        public int TrainingHours { get; set; }

        public string? Description { get; set; }

        [Required]
        [ForeignKey(nameof(University))]
        public int UniversityId { get; set; }

        public University? University { get; set; }

        public User? Student { get; set; }

        public string? UploadedFilePath { get; set; }
        public string? UploadedFileName { get; set; }
        public string? UploadedFileHash { get; set; }
    }
}
