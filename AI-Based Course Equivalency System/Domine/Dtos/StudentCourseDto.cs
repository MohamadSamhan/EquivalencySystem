using System.ComponentModel.DataAnnotations;

namespace Domine.Dtos
{
    public class StudentCourseDto
    {
        public int Id { get; set; }
        public string CourseName { get; set; } = null!;
        public string? ProviderName { get; set; }
        public int CreditHours { get; set; }
        public int TrainingHours { get; set; }
        public string? Description { get; set; }
        public int UniversityId { get; set; }
        public string? UniversityName { get; set; }
        public string? UploadedFileName { get; set; }
        public string? UploadedFileUrl { get; set; }
    }

    public class CreateStudentCourseDto
    {
        [Required]
        [MaxLength(250)]
        public string CourseName { get; set; } = null!;

        [MaxLength(300)]
        public string? ProviderName { get; set; }

        public int CreditHours { get; set; }

        public int TrainingHours { get; set; }

        public string? Description { get; set; }

        [Required]
        public int UniversityId { get; set; }
    }
}
