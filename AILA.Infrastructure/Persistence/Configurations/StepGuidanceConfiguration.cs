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
    public class StepGuidanceConfiguration : IEntityTypeConfiguration<StepGuidance>
    {
        public void Configure(EntityTypeBuilder<StepGuidance> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.AIPracticeMaterialId,
                x.OrderIndex
            }).IsUnique();

            builder.Property(x => x.Id)
                   .ValueGeneratedNever();

            builder.Property(x => x.AIPracticeMaterialId)
                   .IsRequired();

            builder.Property(x => x.OrderIndex)
                   .IsRequired();

            builder.Property(x => x.Content)
                   .IsRequired();

            builder.HasOne(x => x.AIPracticeMaterial)
                   .WithMany(x => x.StepGuidances)
                   .HasForeignKey(x => x.AIPracticeMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
