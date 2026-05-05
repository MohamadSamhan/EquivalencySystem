using Domine.Entity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domine.Dtos
{
    // Single course extracted from transcript PDF
    public class TranscriptCourseDto
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int CreditHours { get; set; }
    }

    // ── NEW: Result of EXTRACT-ONLY step (no DB save, no comparison) ─────────
    public class TranscriptExtractResultDto
    {
        public int TransferRequestId { get; set; }
        public string TransferType { get; set; } = string.Empty;
        public List<TranscriptCourseDto> Courses { get; set; } = new();
    }

    // Result of evaluating one extracted course against internal DB
    public class TranscriptCourseEvaluationDto
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int CreditHours { get; set; }
        public bool Passed { get; set; }
        public bool Skipped { get; set; }
        public int? MatchedCourseId { get; set; }
        public string? MatchedCourseName { get; set; }
        public double SimilarityScore { get; set; }
        public string Decision { get; set; } = string.Empty;
        public int? EquivalencyRequestId { get; set; }
    }

    // Full response for transcript evaluation
    public class TranscriptEvaluationResultDto
    {
        public int TransferRequestId { get; set; }
        public string TransferType { get; set; } = string.Empty;
        public int TotalExtracted { get; set; }
        public int TotalPassed { get; set; }
        public int TotalSkipped { get; set; }
        public List<TranscriptCourseEvaluationDto> Results { get; set; } = new();
    }

    // ── NEW: Request to submit extracted courses for evaluation ──────────────
    public class SubmitExtractedCoursesDto
    {
        public int TransferRequestId { get; set; }
        public List<TranscriptCourseDto> Courses { get; set; } = new();
    }

    // ── 1. Internal Transfer ──────────────────────────────────────────────────
    public class InternalTransferRequestDto
    {
        [Required]
        public IFormFile TranscriptFile { get; set; } = null!;
    }

    // ── 2. External Jordanian Transfer ───────────────────────────────────────
    public class ExternalJordanianTransferRequestDto
    {
        [Required]
        public int UniversityId { get; set; }

        [Required]
        public int FacultyId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string OldStudentId { get; set; } = string.Empty;

        [Required]
        public IFormFile TranscriptFile { get; set; } = null!;
    }

    // ── 3. External Non-Jordanian Transfer ───────────────────────────────────
    public class ExternalNonJordanianTransferRequestDto
    {
        [Required]
        [MaxLength(300)]
        public string UniversityName { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string MajorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string OldStudentId { get; set; } = string.Empty;

        [Required]
        public IFormFile TranscriptFile { get; set; } = null!;
    }
}
