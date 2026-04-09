using Domine.Dtos;
using Domine.Entity;
using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _config = config;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return null;

            // Replace this with proper hashed verification in production.
            // Example uses PasswordHasher; uncomment and use when passwords are hashed.
            // var hasher = new PasswordHasher<User>();
            // var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            // if (result == PasswordVerificationResult.Failed) return null;

            // Temporary plain-text comparison (only for local/dev)
            if (user.PasswordHash != dto.Password)
                return null;

            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var m) ? m : 60;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString()),
                new Claim("fullName", user.FullName)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponseDto
            {
                Token = tokenString,
                Role = user.Role.ToString(),
                FullName = user.FullName
            };
        }
    }
}
