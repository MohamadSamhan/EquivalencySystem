using System.ComponentModel.DataAnnotations;
using Domine.Enum;

namespace Domine.Dtos
{
    public class CreateEquivalencyRequestDto
    {
        [Required]
        public int StudentCourseId { get; set; }

        [Required]
        public int TargetCourseId { get; set; }
    }

    public class EquivalencyRequestDto
    {
        public int Id { get; set; }

        public int StudentCourseId { get; set; }

        public int TargetCourseId { get; set; }

        // Integer percent (0-100) kept for storage/backwards compatibility
        public int SimilarityScore { get; set; }

        // Normalized similarity for frontend convenience (0.0 - 1.0)
        public double Similarity { get; set; }

        public RequestStatus Status { get; set; }

        // Suggestion derived from similarity:
        // "AutoApproved", "RecommendToDoctor", "ManualEvaluation"
        public string Suggestion { get; set; } = string.Empty;

        public string CreatedAt { get; set; } = string.Empty;

        // Student info
        public string StudentName { get; set; } = string.Empty;

        // Course names
        public string StudentCourseName { get; set; } = string.Empty;
        public string TargetCourseName { get; set; } = string.Empty;

        // File download URL for doctor to view student's uploaded PDF
        public string? StudentFileUrl { get; set; }
        public string? StudentFileName { get; set; }
    }
}

