using Domine.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domine.Entity
{
    public enum TransferType
    {
        Internal = 1,
        ExternalJordanian = 2,
        ExternalNonJordanian = 3
    }

    public class TransferRequest
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Student))]
        public int StudentId { get; set; }

        [Required]
        public TransferType TransferType { get; set; }

        // ── ExternalJordanian only ──────────────────
        public int? UniversityId { get; set; }
        public int? FacultyId { get; set; }
        public int? DepartmentId { get; set; }

        // ── ExternalNonJordanian only ───────────────
        [MaxLength(300)]
        public string? UniversityName { get; set; }

        [MaxLength(300)]
        public string? MajorName { get; set; }

        // ── Shared ──────────────────────────────────
        [MaxLength(100)]
        public string? OldStudentId { get; set; }

        // Relative path to saved transcript PDF
        [MaxLength(500)]
        public string? TranscriptFilePath { get; set; }

        [MaxLength(500)]
        public string? TranscriptFileName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Student { get; set; }
        public University? University { get; set; }
    }
}
