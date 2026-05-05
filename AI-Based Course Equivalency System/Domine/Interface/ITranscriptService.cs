using Domine.Dtos;
using Domine.Entity;
using Domine.Enum;
using System.IO;
using System.Threading.Tasks;

namespace Domine.Interface
{
    public interface ITranscriptService
    {
        // ── Step 1: Extract courses only (no evaluation, no DB save for requests) ──
        Task<TranscriptExtractResultDto> ExtractOnlyAsync(
            int studentId,
            TransferType transferType,
            int? universityId,
            int? facultyId,
            int? departmentId,
            string? oldStudentId,
            string? universityName,
            string? majorName,
            Stream transcriptPdfStream,
            string originalFileName);

        // ── Step 2: Submit extracted courses for evaluation & save ────────────
        Task<TranscriptEvaluationResultDto> SubmitExtractedCoursesAsync(
            int studentId,
            SubmitExtractedCoursesDto dto);

        // ── Legacy: full pipeline ─────────────────────────────────────────────
        Task<TranscriptEvaluationResultDto> ProcessInternalAsync(
            int studentId,
            Stream transcriptPdfStream,
            string originalFileName);

        Task<TranscriptEvaluationResultDto> ProcessExternalJordanianAsync(
            int studentId,
            int universityId,
            int facultyId,
            int departmentId,
            string oldStudentId,
            Stream transcriptPdfStream,
            string originalFileName);

        Task<TranscriptEvaluationResultDto> ProcessExternalNonJordanianAsync(
            int studentId,
            string universityName,
            string majorName,
            string oldStudentId,
            Stream transcriptPdfStream,
            string originalFileName);
    }
}
