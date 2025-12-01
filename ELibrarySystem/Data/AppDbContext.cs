using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using ELibrarySystem.Models;

namespace ELibrarySystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets for all entities
        public DbSet<School> Schools { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Standard> Standards { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<SchoolUser> SchoolUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure School entity
            modelBuilder.Entity<School>()
                .HasKey(s => s.SchoolId);
            modelBuilder.Entity<School>()
                .HasIndex(s => s.SchoolCode)
                .IsUnique();

            // Configure Teacher entity
            modelBuilder.Entity<Teacher>()
                .HasKey(t => t.TeacherId);
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.School)
                .WithMany(s => s.Teachers)
                .HasForeignKey(t => t.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Student entity
            modelBuilder.Entity<Student>()
                .HasKey(s => s.StudentId);
            modelBuilder.Entity<Student>()
                .HasOne(s => s.School)
                .WithMany(sch => sch.Students)
                .HasForeignKey(s => s.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Standard)
                .WithMany(st => st.Students)
                .HasForeignKey(s => s.StandardId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Division)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.DivisionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Standard entity
            modelBuilder.Entity<Standard>()
                .HasKey(st => st.StandardId);

            // Configure Division entity
            modelBuilder.Entity<Division>()
                .HasKey(d => d.DivisionId);

            // Configure SchoolUser entity
            modelBuilder.Entity<SchoolUser>()
                .HasKey(u => u.UserId);
            modelBuilder.Entity<SchoolUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<SchoolUser>()
                .HasOne(u => u.School)
                .WithMany(s => s.SchoolUsers)
                .HasForeignKey(u => u.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SchoolUser>()
                .HasOne(u => u.Student)
                .WithOne(s => s.SchoolUser)
                .HasForeignKey<SchoolUser>(u => u.StudentId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<SchoolUser>()
                .HasOne(u => u.Teacher)
                .WithMany(t => t.SchoolUsers)
                .HasForeignKey(u => u.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}