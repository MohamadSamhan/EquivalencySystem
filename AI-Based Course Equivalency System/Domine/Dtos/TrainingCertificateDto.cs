using Domine.Enum;
using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Domine.Dtos
{
    // Student submits this form
    public class CreateTrainingCertificateRequestDto
    {
        [Required]
        [MaxLength(300)]
        public string TrainingTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string TrainingProvider { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000)]
        public int TrainingHours { get; set; }

        [Required]
        public IFormFile CertificateFile { get; set; } = null!;
    }

    // Returned to Doctor / Admin / Student
    public class TrainingCertificateRequestDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string TrainingTitle { get; set; } = string.Empty;
        public string TrainingProvider { get; set; } = string.Empty;
        public int TrainingHours { get; set; }
        public string? CertificateFileUrl { get; set; }
        public string? CertificateFileName { get; set; }
        public RequestStatus Status { get; set; }
        public string? ReviewerNotes { get; set; }
        public string? ReviewedByDoctorName { get; set; }
        public string? ReviewedAt { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
