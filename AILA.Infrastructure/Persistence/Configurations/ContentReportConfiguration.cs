using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class ContentReportConfiguration
        : IEntityTypeConfiguration<ContentReport>
    {
        public void Configure(EntityTypeBuilder<ContentReport> builder)
        {
            builder.ToTable("ContentReport");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.LearnerId,
                x.CourseId,
                x.MaterialId
            })
            .IsUnique();

            builder.Property(x => x.CourseId)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(x => x.ReportType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.Description);

            builder.HasOne(x => x.Learner)
                .WithMany()
                .HasForeignKey(x => x.LearnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
