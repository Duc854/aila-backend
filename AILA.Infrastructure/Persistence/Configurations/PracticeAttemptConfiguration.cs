using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class PracticeAttemptConfiguration : IEntityTypeConfiguration<PracticeAttempt>
    {
        public void Configure(EntityTypeBuilder<PracticeAttempt> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .ValueGeneratedNever();

            builder.Property(x => x.EnrollmentId)
                   .IsRequired();

            builder.Property(x => x.MaterialId)
                   .IsRequired();

            builder.Property(x => x.OverallSuggestion)
                   .HasDefaultValue(string.Empty);

            builder.Ignore(x => x.Submissions);

            builder.HasOne(x => x.Enrollment)
                   .WithMany()
                   .HasForeignKey(x => x.EnrollmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Material)
                   .WithMany()
                   .HasForeignKey(x => x.MaterialId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.EnrollmentId);

            builder.HasIndex(x => x.MaterialId);

            builder.HasIndex(x => new
            {
                x.EnrollmentId,
                x.MaterialId
            });
        }
    }
}
