using Domine.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domine.Entity
{
    public class TrainingCertificateRequest
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Student))]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(300)]
        public string TrainingTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string TrainingProvider { get; set; } = string.Empty;

        [Required]
        public int TrainingHours { get; set; }

        [MaxLength(500)]
        public string? CertificateFilePath { get; set; }

        [MaxLength(500)]
        public string? CertificateFileName { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [ForeignKey(nameof(ReviewedByDoctor))]
        public int? ReviewedByDoctorId { get; set; }

        [MaxLength(1000)]
        public string? ReviewerNotes { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Student { get; set; }
        public User? ReviewedByDoctor { get; set; }
    }
}
