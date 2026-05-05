using Domine.Dtos;
using Domine.Entity;
using Domine.Enum;
using Domine.Interface;
using Infrastacture;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;

        public AdminService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===================== USER MANAGEMENT =====================

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            return await _db.Users
                .OrderBy(u => u.FullName)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role.ToString()
                })
                .ToListAsync();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            // Check for duplicate email
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                throw new InvalidOperationException($"A user with email '{dto.Email}' already exists.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = dto.Password, // TODO: Replace with proper hashing in production
                Role = dto.Role
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            };
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            // Only update fields that were provided (partial update)
            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName;

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                // Check that the new email isn't taken by another user
                var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailTaken)
                    throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");
                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = dto.Password; // TODO: Hash in production

            if (dto.Role.HasValue)
                user.Role = dto.Role.Value;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            // Prevent deleting admin users
            if (user.Role == UserRole.Admin)
                throw new InvalidOperationException("Cannot delete an admin user.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return true;
        }

        // ===================== UNIVERSITY MANAGEMENT =====================

        public async Task<IEnumerable<UniversityDto>> GetAllUniversitiesAsync()
        {
            return await _db.Universities
                .OrderBy(u => u.Name)
                .Select(u => new UniversityDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Country = u.Country
                })
                .ToListAsync();
        }

        public async Task<UniversityDto?> GetUniversityByIdAsync(int id)
        {
            var uni = await _db.Universities.FindAsync(id);
            if (uni == null) return null;

            return new UniversityDto
            {
                Id = uni.Id,
                Name = uni.Name,
                Country = uni.Country
            };
        }

        public async Task<UniversityDto> CreateUniversityAsync(CreateUniversityDto dto)
        {
            var uni = new University
            {
                Name = dto.Name,
                Country = dto.Country
            };

            _db.Universities.Add(uni);
            await _db.SaveChangesAsync();

            return new UniversityDto
            {
                Id = uni.Id,
                Name = uni.Name,
                Country = uni.Country
            };
        }

        public async Task<bool> UpdateUniversityAsync(int id, UpdateUniversityDto dto)
        {
            var uni = await _db.Universities.FindAsync(id);
            if (uni == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.Name))
                uni.Name = dto.Name;

            if (dto.Country != null) // Allow setting to empty string
                uni.Country = dto.Country;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUniversityAsync(int id)
        {
            var uni = await _db.Universities
                .Include(u => u.StudentCourses)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (uni == null) return false;

            // Prevent deletion if university has associated student courses
            if (uni.StudentCourses != null && uni.StudentCourses.Any())
                throw new InvalidOperationException(
                    "Cannot delete university because it has associated student courses. Remove them first.");

            _db.Universities.Remove(uni);
            await _db.SaveChangesAsync();
            return true;
        }

        // ===================== COURSE MANAGEMENT =====================

        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync()
        {
            return await _db.Courses
                .OrderBy(c => c.CourseName)
                .Select(c => new CourseDto
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    CreditHours = c.CreditHours,
                    Description = c.Description
                })
                .ToListAsync();
        }

        public async Task<CourseDto?> GetCourseByIdAsync(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return null;

            return new CourseDto
            {
                Id = course.Id,
                CourseName = course.CourseName,
                CreditHours = course.CreditHours,
                Description = course.Description
            };
        }

        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto)
        {
            var course = new Course
            {
                CourseName = dto.CourseName,
                CreditHours = dto.CreditHours,
                Description = dto.Description
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            return new CourseDto
            {
                Id = course.Id,
                CourseName = course.CourseName,
                CreditHours = course.CreditHours,
                Description = course.Description
            };
        }

        public async Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.CourseName))
                course.CourseName = dto.CourseName;

            if (dto.CreditHours.HasValue)
                course.CreditHours = dto.CreditHours.Value;

            if (dto.Description != null)
                course.Description = dto.Description;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null) return false;

            // Check if any equivalency requests reference this course
            var hasRequests = await _db.EquivalencyRequests
                .AnyAsync(r => r.TargetCourseId == id);

            if (hasRequests)
                throw new InvalidOperationException(
                    "Cannot delete course because it is referenced by equivalency requests.");

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
            return true;
        }

        // ===================== DASHBOARD =====================

        public async Task<AdminDashboardDto> GetDashboardAsync()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalStudents = await _db.Users.CountAsync(u => u.Role == UserRole.Student);
            var totalDoctors = await _db.Users.CountAsync(u => u.Role == UserRole.Doctor);
            var totalUniversities = await _db.Universities.CountAsync();
            var totalCourses = await _db.Courses.CountAsync();
            var totalRequests = await _db.EquivalencyRequests.CountAsync();
            var pendingRequests = await _db.EquivalencyRequests
                .CountAsync(r => r.Status == RequestStatus.Pending);
            var approvedRequests = await _db.EquivalencyRequests
                .CountAsync(r => r.Status == RequestStatus.Approved);
            var rejectedRequests = await _db.EquivalencyRequests
                .CountAsync(r => r.Status == RequestStatus.Rejected);

            return new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalStudents = totalStudents,
                TotalDoctors = totalDoctors,
                TotalUniversities = totalUniversities,
                TotalCourses = totalCourses,
                TotalRequests = totalRequests,
                PendingRequests = pendingRequests,
                ApprovedRequests = approvedRequests,
                RejectedRequests = rejectedRequests
            };
        }
    }
}
