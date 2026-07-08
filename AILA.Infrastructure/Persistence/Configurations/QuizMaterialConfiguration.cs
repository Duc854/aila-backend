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
    public class QuizMaterialConfiguration : IEntityTypeConfiguration<QuizMaterial>
    {
        public void Configure(EntityTypeBuilder<QuizMaterial> builder)
        {
            builder.HasKey(x => x.MaterialId);

            builder.Property(x => x.PassingScore)
                .HasPrecision(5, 2);

            builder.HasMany(x => x.Questions)
                .WithOne(x => x.QuizMaterial)
                .HasForeignKey(x => x.QuizMaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Questions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.QuizAttempts)
                .WithOne(x => x.QuizMaterial)
                .HasForeignKey(x => x.QuizMaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.QuizAttempts)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
