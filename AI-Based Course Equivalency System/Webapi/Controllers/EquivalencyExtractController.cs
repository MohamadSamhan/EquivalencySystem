using Domine.Dtos;
using Domine.Entity;
using Domine.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Webapi.Controllers
{
    [Route("api/equivalency")]
    [ApiController]
    public class EquivalencyExtractController : ControllerBase
    {
        private readonly ITranscriptService _transcriptService;
        private readonly ILogger<EquivalencyExtractController> _logger;
        private readonly IConfiguration _config;

        public EquivalencyExtractController(
            ITranscriptService transcriptService,
            ILogger<EquivalencyExtractController> logger,
            IConfiguration config)
        {
            _transcriptService = transcriptService;
            _logger = logger;
            _config = config;
        }

        // POST /api/equivalency/extract-courses
        [HttpPost("extract-courses")]
        [AllowAnonymous]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> ExtractCourses([FromForm] ExtractCoursesRequestDto dto)
        {
            // ── 1. Manually validate JWT from Authorization header ────────────
            var principal = GetPrincipalFromAuthHeader();
            if (principal == null)
                return Unauthorized(new { Message = "Missing or invalid Bearer token." });

            // ── 2. Check role is Student ──────────────────────────────────────
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { Message = $"Access denied. Required role: Student. Your role: {role}" });

            // ── 3. Extract student ID ─────────────────────────────────────────
            var studentId = GetUserIdFromPrincipal(principal);
            if (studentId == null)
                return Unauthorized(new { Message = "Could not resolve student ID from token." });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            // ── 4. Process transcript ─────────────────────────────────────────
            try
            {
                await using var stream = dto.TranscriptFile.OpenReadStream();
                var fileName = dto.TranscriptFile.FileName;

                TranscriptEvaluationResultDto result = dto.TransferType?.ToLower() switch
                {
                    "external-jordanian" => await _transcriptService.ProcessExternalJordanianAsync(
                        studentId.Value,
                        dto.UniversityId ?? 1,
                        dto.FacultyId ?? 1,
                        dto.DepartmentId ?? 1,
                        dto.OldStudentId ?? string.Empty,
                        stream,
                        fileName),

                    "external-non-jordanian" => await _transcriptService.ProcessExternalNonJordanianAsync(
                        studentId.Value,
                        dto.UniversityName ?? string.Empty,
                        dto.MajorName ?? string.Empty,
                        dto.OldStudentId ?? string.Empty,
                        stream,
                        fileName),

                    // default: internal
                    _ => await _transcriptService.ProcessInternalAsync(studentId.Value, stream, fileName)
                };

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Extract-courses failed for student {Id}", studentId);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in extract-courses for student {Id}", studentId);
                return StatusCode(500, new { Message = "An error occurred processing the transcript." });
            }
        }

        // POST /api/equivalency/extract-only  ← الخطوة 1: استخراج فقط
        [HttpPost("extract-only")]
        [AllowAnonymous]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> ExtractOnly([FromForm] ExtractCoursesRequestDto dto)
        {
            var principal = GetPrincipalFromAuthHeader();
            if (principal == null)
                return Unauthorized(new { Message = "Missing or invalid Bearer token." });

            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { Message = "Access denied." });

            var studentId = GetUserIdFromPrincipal(principal);
            if (studentId == null)
                return Unauthorized(new { Message = "Could not resolve student ID from token." });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await using var stream = dto.TranscriptFile.OpenReadStream();
                var transferType = dto.TransferType?.ToLower() switch
                {
                    "external-jordanian"     => TransferType.ExternalJordanian,
                    "external-non-jordanian" => TransferType.ExternalNonJordanian,
                    _                        => TransferType.Internal
                };

                var result = await _transcriptService.ExtractOnlyAsync(
                    studentId.Value,
                    transferType,
                    dto.UniversityId,
                    dto.FacultyId,
                    dto.DepartmentId,
                    dto.OldStudentId,
                    dto.UniversityName,
                    dto.MajorName,
                    stream,
                    dto.TranscriptFile.FileName);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "extract-only failed for student {Id}", studentId);
                return StatusCode(500, new { Message = "An error occurred processing the transcript." });
            }
        }

        // POST /api/equivalency/submit-courses  ← الخطوة 2: مقارنة وحفظ
        [HttpPost("submit-courses")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitCourses([FromBody] SubmitExtractedCoursesDto dto)
        {
            var principal = GetPrincipalFromAuthHeader();
            if (principal == null)
                return Unauthorized(new { Message = "Missing or invalid Bearer token." });

            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { Message = "Access denied." });

            var studentId = GetUserIdFromPrincipal(principal);
            if (studentId == null)
                return Unauthorized(new { Message = "Could not resolve student ID from token." });

            try
            {
                var result = await _transcriptService.SubmitExtractedCoursesAsync(studentId.Value, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "submit-courses failed for student {Id}", studentId);
                return StatusCode(500, new { Message = "An error occurred." });
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Reads and validates the Bearer token from the Authorization header.</summary>
        private ClaimsPrincipal? GetPrincipalFromAuthHeader()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            var token = authHeader["Bearer ".Length..].Trim();
            return ValidateToken(token);
        }

        /// <summary>Validates a JWT using the same config as the app middleware.</summary>
        private ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var jwtSection = _config.GetSection("Jwt");
                var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
                var issuer = jwtSection["Issuer"];
                var audience = jwtSection["Audience"];

                var handler = new JwtSecurityTokenHandler();
                handler.InboundClaimTypeMap.Clear();

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };

                return handler.ValidateToken(token, parameters, out _);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                return null;
            }
        }

        private static int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                     ?? principal.FindFirst("sub")?.Value;

            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
