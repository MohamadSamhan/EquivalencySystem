using Domine.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Entity
{
    public class EquivalencyRequest
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Student))]
        public int StudentId { get; set; }

        [Required]
        [ForeignKey(nameof(StudentCourse))]
        public int StudentCourseId { get; set; }

        [Required]
        [ForeignKey(nameof(TargetCourse))]
        public int TargetCourseId { get; set; }

        public int SimilarityScore { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Student { get; set; }

        public StudentCourse? StudentCourse { get; set; }
        public Course? TargetCourse { get; set; }
    }
}
