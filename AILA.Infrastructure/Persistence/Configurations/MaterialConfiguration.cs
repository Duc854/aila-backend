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
    public class MaterialConfiguration : IEntityTypeConfiguration<Material>
    {
        public void Configure(EntityTypeBuilder<Material> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.MaterialType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasOne(x => x.QuizDetails)
                .WithOne(x => x.Material)
                .HasForeignKey<QuizMaterial>(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.VideoDetails)
                .WithOne(x => x.Material)
                .HasForeignKey<VideoMaterial>(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.DocumentDetails)
                .WithOne(x => x.Material)
                .HasForeignKey<DocumentMaterial>(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.AIPracticeDetails)
              .WithOne(a => a.Material)
              .HasForeignKey<AIPracticeMaterial>(a => a.MaterialId)
              .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
