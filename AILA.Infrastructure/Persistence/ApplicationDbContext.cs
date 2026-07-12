using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

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
        public DbSet<QuizMaterial> QuizMaterials => Set<QuizMaterial>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
        public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
        public DbSet<AIPracticeMaterial> AIPracticeMaterials => Set<AIPracticeMaterial>();
        public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
        public DbSet<StepGuidance> StepGuidances => Set<StepGuidance>();
        public DbSet<ScoringCriteria> ScoringCriterias => Set<ScoringCriteria>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}