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
    public class AIPracticeMaterialConfiguration : IEntityTypeConfiguration<AIPracticeMaterial>
    {
        public void Configure(EntityTypeBuilder<AIPracticeMaterial> builder)
        {
            builder.HasKey(x => x.MaterialId);

            builder.Property(x => x.Scenario)
                   .IsRequired()
                   .HasColumnType("text");

            builder.Property(x => x.AITask)
                   .IsRequired()
                   .HasColumnType("text");
            builder.Property(x => x.LearnerTask)
                   .IsRequired()
                   .HasColumnType("text");

            builder.Property(x => x.Difficulty)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(x => x.MaxPromptAttempts)
                   .IsRequired();



            builder.HasMany(x => x.PromptTemplates)
                   .WithOne(x => x.AIPracticeMaterial)
                   .HasForeignKey(x => x.AIPracticeMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(x => x.StepGuidances)
                   .WithOne(x => x.AIPracticeMaterial)
                   .HasForeignKey(x => x.AIPracticeMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ScoringCriterias)
                   .WithOne(x => x.AIPracticeMaterial)
                   .HasForeignKey(x => x.AIPracticeMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.PromptTemplates)
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(x => x.StepGuidances)
                   .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(x => x.ScoringCriterias)
                   .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }

}
