using Domine.Enum;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domine.Dtos
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;

        [Required]
        public UserRole Role { get; set; }
    }

    public class UpdateUserDto
    {
        [MaxLength(200)]
        public string? FullName { get; set; }

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }

        public UserRole? Role { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }

    // ===== University Management DTOs =====

    public class CreateUniversityDto
    {
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Country { get; set; }
    }

    public class UpdateUniversityDto
    {
        [MaxLength(250)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }
    }

    public class UniversityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Country { get; set; }
    }

    // ===== Course Management DTOs =====

    public class UpdateCourseDto
    {
        [MaxLength(250)]
        public string? CourseName { get; set; }

        public int? CreditHours { get; set; }

        public string? Description { get; set; }
    }

    /// <summary>
    /// يقبل Role كرقم أو نص: "Student"/"Doctor"/"Admin" أو "طالب"/"دكتور"
    /// </summary>
    public class UserRoleConverter : JsonConverter<UserRole>
    {
        public override UserRole Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
                return (UserRole)reader.GetInt32();

            var str = reader.GetString() ?? string.Empty;

            // طباعة القيمة للـ debugging
            Console.WriteLine($"[UserRoleConverter] Received role value: '{str}'");

            return str.ToLower().Trim() switch
            {
                "student" or "طالب" or "0"    => UserRole.Student,
                "doctor"  or "دكتور" or "1"   => UserRole.Doctor,
                "admin"   or "مدير" or "2"    => UserRole.Admin,
                _ => System.Enum.TryParse<UserRole>(str, true, out var r) ? r : UserRole.Student
            };
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, UserRole value, System.Text.Json.JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
