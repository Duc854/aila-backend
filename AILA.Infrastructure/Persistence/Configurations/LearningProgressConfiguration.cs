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
    public class LearningProgressConfiguration : IEntityTypeConfiguration<LearningProgress>
    {
        public void Configure(EntityTypeBuilder<LearningProgress> builder)
        {
            builder.HasKey(x => new
            {
                x.EnrollmentId,
                x.MaterialId
            });

            builder.HasOne(x => x.Enrollment)
                .WithMany()
                .HasForeignKey(x => x.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
