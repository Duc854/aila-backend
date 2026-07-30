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
    public class CourseReviewRequestConfiguration
        : IEntityTypeConfiguration<CourseReviewRequest>
    {
        public void Configure(EntityTypeBuilder<CourseReviewRequest> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CourseId)
                .IsRequired();

            builder.Property(x => x.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.ReviewComment)
                .HasMaxLength(1000);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.HasOne(x => x.Course)
                .WithMany(x => x.ReviewRequests)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
