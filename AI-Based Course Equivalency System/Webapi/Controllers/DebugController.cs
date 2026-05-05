using Domine.Entity;
using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ISimilarityService _sim;
        private readonly ILogger<DebugController> _logger;
        private readonly IConfiguration _config;

        public DebugController(
            ApplicationDbContext db,
            IWebHostEnvironment env,
            ISimilarityService sim,
            ILogger<DebugController> logger,
            IConfiguration config)
        {
            _db = db;
            _env = env;
            _sim = sim;
            _logger = logger;
            _config = config;
        }

        // GET /api/debug/preview/{studentCourseId}/{targetCourseId}
        [HttpGet("preview/{studentCourseId}/{targetCourseId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Preview(int studentCourseId, int targetCourseId)
        {
            var sc = await _db.StudentCourses.FindAsync(studentCourseId);
            var tc = await _db.Courses.FindAsync(targetCourseId);
            if (sc == null || tc == null) return NotFound();

            string studentText = await ReadPdfTextIfExists(sc.UploadedFilePath);
            string targetText = await ReadPdfTextIfExists(tc.ReferenceFilePath);

            _logger.LogInformation("Preview: studentTextLen={Len1} targetTextLen={Len2}", studentText.Length, targetText.Length);

            var sim = _sim.CalculateSimilarity(studentText, targetText);

            return Ok(new
            {
                StudentCourseId = sc.Id,
                UploadedPath = sc.UploadedFilePath,
                StudentTextLength = studentText.Length,
                StudentSample = studentText.Length > 400 ? studentText.Substring(0, 400) : studentText,
                TargetCourseId = tc.Id,
                ReferencePath = tc.ReferenceFilePath,
                TargetTextLength = targetText.Length,
                TargetSample = targetText.Length > 400 ? targetText.Substring(0, 400) : targetText,
                Similarity = sim
            });
        }

        // GET /api/debug/test-openai
        // Generates a short-lived JWT internally (Student role) and uses it to verify
        // that authentication + OpenAI connectivity are both working end-to-end.
        [HttpGet("test-openai")]
        [AllowAnonymous]
        public async Task<IActionResult> TestOpenAi()
        {
            // ── 1. Generate an internal test JWT with Student role ──────────
            string internalToken;
            try
            {
                internalToken = GenerateInternalTestToken();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate internal test JWT");
                return StatusCode(500, new { Status = "Error", Message = "JWT configuration error: " + ex.Message });
            }

            // ── 2. Validate the generated token to confirm JWT setup is OK ──
            var principal = ValidateToken(internalToken);
            if (principal == null)
            {
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "Generated JWT could not be validated. Check Jwt:Key, Issuer, and Audience in appsettings.json."
                });
            }

            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            // ── 3. Test OpenAI connectivity ──────────────────────────────────
            var testText = "This is a test to verify OpenAI API connectivity and embeddings";
            _logger.LogInformation("Testing OpenAI API with sample text: {Text}", testText);

            try
            {
                var embedding = await _sim.GetEmbeddingAsync(testText);
                if (embedding == null)
                {
                    return StatusCode(500, new
                    {
                        Status = "Failed",
                        JwtStatus = "OK",
                        TokenRole = role,
                        Message = "OpenAI API returned null embedding.",
                        Suggestion = "Check: 1) API key validity, 2) Account credits, 3) Network connectivity"
                    });
                }

                return Ok(new
                {
                    Status = "Success",
                    JwtStatus = "OK",
                    TokenRole = role,
                    Message = "JWT and OpenAI API are both working correctly",
                    EmbeddingLength = embedding.Length,
                    FirstTenValues = embedding.Take(10).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI API test failed");
                return StatusCode(500, new
                {
                    Status = "Error",
                    JwtStatus = "OK",
                    TokenRole = role,
                    Message = "OpenAI API test failed: " + ex.Message,
                    Suggestion = "Check your OpenAI API key in appsettings.json"
                });
            }
        }

        // POST /api/debug/echo-user  ← لمعرفة ماذا يرسل الـ frontend
        [HttpPost("echo-user")]
        [AllowAnonymous]
        public async Task<IActionResult> EchoUser()
        {
            using var reader = new System.IO.StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            return Ok(new { ReceivedBody = body });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Generates a short-lived JWT with Student role for internal testing.</summary>
        private string GenerateInternalTestToken()
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "0"),
                new Claim(ClaimTypes.NameIdentifier, "0"),
                new Claim(ClaimTypes.Role, "Student"),
                new Claim("fullName", "Debug Test User")
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Validates a JWT using the same parameters as the app middleware.</summary>
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
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };

                return handler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> ReadPdfTextIfExists(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
            var trimmed = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                      .Replace("/", Path.DirectorySeparatorChar.ToString());
            var full = Path.Combine(_env.ContentRootPath, trimmed);
            if (!System.IO.File.Exists(full)) return string.Empty;

            var sb = new StringBuilder();
            using var doc = PdfDocument.Open(full);
            foreach (var page in doc.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrEmpty(text)) sb.AppendLine(text);
            }
            return sb.ToString();
        }
    }
}
