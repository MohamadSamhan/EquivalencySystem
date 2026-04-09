using Domine.Entity;
using Microsoft.EntityFrameworkCore;

namespace Infrastacture
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<StudentCourse> StudentCourses => Set<StudentCourse>();
        public DbSet<EquivalencyRequest> EquivalencyRequests => Set<EquivalencyRequest>();
        public DbSet<University> Universities => Set<University>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasMany(u => u.StudentCourses)
                .WithOne(sc => sc.Student)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.EquivalencyRequests)
                .WithOne(er => er.Student)
                .HasForeignKey(er => er.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.University)
                .WithMany(u => u.StudentCourses)
                .HasForeignKey(sc => sc.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EquivalencyRequest>()
                .HasOne(er => er.StudentCourse)
                .WithMany()
                .HasForeignKey(er => er.StudentCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EquivalencyRequest>()
                .HasOne(er => er.TargetCourse)
                .WithMany()
                .HasForeignKey(er => er.TargetCourseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
