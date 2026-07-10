using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport>
    {
        public void Configure(EntityTypeBuilder<ContentReport> builder)
        {
            // Pin tên bảng (số ít) khớp với migration đã tạo — tránh việc thêm DbSet<ContentReport>
            // (số nhiều) sau này làm EF đổi tên bảng và lệch với DB.
            builder.ToTable("ContentReport");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.LearnerId,
                x.CourseId,
                x.MaterialId
            }).IsUnique();

            builder.Property(x => x.Status)
               .HasConversion<string>()
               .HasMaxLength(30);

            builder.Property(x => x.ResolvedAt);

            builder.Property(x => x.ReportType)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(x => x.Description);

            builder.HasOne(x => x.Learner)
                   .WithMany()
                   .HasForeignKey(x => x.LearnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Course)
                   .WithMany()
                   .HasForeignKey(x => x.CourseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Material)
                   .WithMany()
                   .HasForeignKey(x => x.MaterialId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasCheckConstraint(
                "CK_ContentReport_Target",
                @"(
                    (""CourseId"" IS NOT NULL AND ""MaterialId"" IS NULL)
                    OR
                    (""CourseId"" IS NULL AND ""MaterialId"" IS NOT NULL)
                )");
        }
    }
}
