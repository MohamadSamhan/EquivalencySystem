using Domine.Dtos;
using Domine.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranscriptController : ControllerBase
    {
        private readonly ITranscriptService _transcriptService;
        private readonly ILogger<TranscriptController> _logger;

        public TranscriptController(ITranscriptService transcriptService, ILogger<TranscriptController> logger)
        {
            _transcriptService = transcriptService;
            _logger = logger;
        }

        // POST /api/transcript/internal
        [HttpPost("internal")]
        [Authorize(Roles = "Student")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Internal([FromForm] InternalTransferRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return await Execute(async studentId =>
            {
                await using var stream = dto.TranscriptFile.OpenReadStream();
                return await _transcriptService.ProcessInternalAsync(studentId, stream, dto.TranscriptFile.FileName);
            });
        }

        // POST /api/transcript/external-jordanian
        [HttpPost("external-jordanian")]
        [Authorize(Roles = "Student")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> ExternalJordanian([FromForm] ExternalJordanianTransferRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return await Execute(async studentId =>
            {
                await using var stream = dto.TranscriptFile.OpenReadStream();
                return await _transcriptService.ProcessExternalJordanianAsync(
                    studentId,
                    dto.UniversityId,
                    dto.FacultyId,
                    dto.DepartmentId,
                    dto.OldStudentId,
                    stream,
                    dto.TranscriptFile.FileName);
            });
        }

        // POST /api/transcript/external-non-jordanian
        [HttpPost("external-non-jordanian")]
        [Authorize(Roles = "Student")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> ExternalNonJordanian([FromForm] ExternalNonJordanianTransferRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return await Execute(async studentId =>
            {
                await using var stream = dto.TranscriptFile.OpenReadStream();
                return await _transcriptService.ProcessExternalNonJordanianAsync(
                    studentId,
                    dto.UniversityName,
                    dto.MajorName,
                    dto.OldStudentId,
                    stream,
                    dto.TranscriptFile.FileName);
            });
        }

        // ── shared error handler ──────────────────────────────────────────────
        private async Task<IActionResult> Execute(Func<int, Task<TranscriptEvaluationResultDto>> action)
        {
            var studentId = GetCurrentUserId();
            if (studentId == null) return Unauthorized();

            try
            {
                var result = await action(studentId.Value);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Transfer processing failed for student {Id}", studentId);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error for student {Id}", studentId);
                return StatusCode(500, new { Message = "An error occurred processing the transcript." });
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id)) return id;
            return null;
        }
    }
}
