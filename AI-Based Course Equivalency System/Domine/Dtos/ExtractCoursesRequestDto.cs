using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Domine.Dtos
{
    public class ExtractCoursesRequestDto
    {
        /// <summary>internal | external-jordanian | external-non-jordanian</summary>
        public string? TransferType { get; set; }

        [Required]
        public IFormFile TranscriptFile { get; set; } = null!;

        // ── ExternalJordanian fields ──
        public int? UniversityId { get; set; }
        public int? FacultyId { get; set; }
        public int? DepartmentId { get; set; }

        // ── Shared ──
        public string? OldStudentId { get; set; }

        // ── ExternalNonJordanian fields ──
        public string? UniversityName { get; set; }
        public string? MajorName { get; set; }
    }
}
