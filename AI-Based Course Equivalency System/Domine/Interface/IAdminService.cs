using Domine.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Interface
{
    public interface IAdminService
    {
        // ===== User Management =====
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);

        // ===== University Management =====
        Task<IEnumerable<UniversityDto>> GetAllUniversitiesAsync();
        Task<UniversityDto?> GetUniversityByIdAsync(int id);
        Task<UniversityDto> CreateUniversityAsync(CreateUniversityDto dto);
        Task<bool> UpdateUniversityAsync(int id, UpdateUniversityDto dto);
        Task<bool> DeleteUniversityAsync(int id);

        // ===== Course Management =====
        Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
        Task<CourseDto?> GetCourseByIdAsync(int id);
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
        Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int id);

        // ===== Dashboard / Statistics =====
        Task<AdminDashboardDto> GetDashboardAsync();
    }

    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalUniversities { get; set; }
        public int TotalCourses { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
    }
}
