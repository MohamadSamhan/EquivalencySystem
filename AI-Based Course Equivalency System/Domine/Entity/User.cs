using Domine.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Entity
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!; 

        [Required]
        public UserRole Role { get; set; }

        public ICollection<StudentCourse>? StudentCourses { get; set; }

        public ICollection<EquivalencyRequest>? EquivalencyRequests
        {
            get; set;
        }
    }
}
