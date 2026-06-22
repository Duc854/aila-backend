using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Learner> Learners => Set<Learner>();
        public DbSet<Expert> Experts => Set<Expert>();
        public DbSet<UserToken> UserTokens => Set<UserToken>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<VideoMaterial> VideoMaterials => Set<VideoMaterial>();
        public DbSet<DocumentMaterial> DocumentMaterials => Set<DocumentMaterial>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<LearningProgress> LearningProgresses => Set<LearningProgress>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. CONFIG USER & ROLES ---
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // --- 2. CONFIG RELATION 1-1 (LEARNER & EXPERT) ---
            modelBuilder.Entity<Learner>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.LearnerType).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.KnowledgeLevel).HasConversion<string>().HasMaxLength(50);

                // 1-1 với User
                entity.HasOne(l => l.User)
                      .WithOne(u => u.Learner)
                      .HasForeignKey<Learner>(l => l.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Expert>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Specialty).HasMaxLength(200);

                // 1-1 với User
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Expert)
                      .HasForeignKey<Expert>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- 3. CONFIG USER TOKENS ---
            modelBuilder.Entity<UserToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.RefreshToken).IsUnique();
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45); // ĐÃ BỎ UNIQUE THEO GÓP Ý
                entity.Property(e => e.UserAgent).HasMaxLength(500);

                entity.HasOne(ut => ut.User)
                      .WithMany()
                      .HasForeignKey(ut => ut.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- 4. CONFIG CATEGORIES & BLOGS & NOTIFICATIONS ---
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<BlogPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.RedirectUrl).HasMaxLength(500);

                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- 5. CONFIG COURSE & MANY-TO-MANY TAGS ---
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ThumbnailUrl).HasMaxLength(512);
                entity.Property(e => e.Level).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.DurationHours).HasPrecision(5, 2);

                entity.HasOne(c => c.Category)
                      .WithMany()
                      .HasForeignKey(c => c.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Expert)
                      .WithMany()
                      .HasForeignKey(c => c.ExpertId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Cấu hình bảng trung gian Many-to-Many giữa Course và Tag tự động
                entity.HasMany(c => c.CourseTags)
                      .WithMany()
                      .UsingEntity<Dictionary<string, object>>(
                          "course_tags_mapping",
                          j => j.HasOne<Tag>().WithMany().HasForeignKey("tag_id"),
                          j => j.HasOne<Course>().WithMany().HasForeignKey("course_id")
                      );
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // --- 6. CONFIG COURE CONTENT STRUCTURE (MODULE & MATERIAL) ---
            modelBuilder.Entity<Module>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);

                entity.HasOne(m => m.Course)
                      .WithMany()
                      .HasForeignKey(m => m.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Material>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.MaterialType).HasConversion<string>().HasMaxLength(50);

                entity.HasOne(m => m.Module)
                      .WithMany()
                      .HasForeignKey(m => m.ModuleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- 7. CONFIG 1-1 DETAIL MATERIALS (VIDEO EMBED & DOCUMENT) ---
            modelBuilder.Entity<VideoMaterial>(entity =>
            {
                entity.HasKey(e => e.MaterialId);
                entity.Property(e => e.VideoUrl).IsRequired().HasMaxLength(512);
                entity.Property(e => e.ThumbnailUrl).HasMaxLength(512);
                entity.Property(e => e.CaptionsUrl).HasMaxLength(512);

                entity.HasOne(v => v.Material)
                      .WithOne(m => m.VideoDetails)
                      .HasForeignKey<VideoMaterial>(v => v.MaterialId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DocumentMaterial>(entity =>
            {
                entity.HasKey(e => e.MaterialId);
                entity.Property(e => e.DocumentUrl).HasMaxLength(512);

                entity.HasOne(d => d.Material)
                      .WithOne(m => m.DocumentDetails)
                      .HasForeignKey<DocumentMaterial>(d => d.MaterialId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- 8. CONFIG ENROLLMENTS & PROGRESS ---
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.ProgressPct).HasPrecision(5, 2);

                entity.HasOne(en => en.Learner)
                      .WithMany()
                      .HasForeignKey(en => en.LearnerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(en => en.Course)
                      .WithMany()
                      .HasForeignKey(en => en.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LearningProgress>(entity =>
            {
                // Khóa chính phức hợp kết hợp từ 2 ID
                entity.HasKey(e => new { e.EnrollmentId, e.MaterialId });

                entity.HasOne(lp => lp.Enrollment)
                      .WithMany()
                      .HasForeignKey(lp => lp.EnrollmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(lp => lp.Material)
                      .WithMany()
                      .HasForeignKey(lp => lp.MaterialId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
