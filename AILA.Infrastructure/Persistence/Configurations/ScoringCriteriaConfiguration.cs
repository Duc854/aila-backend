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
    public class ScoringCriteriaConfiguration : IEntityTypeConfiguration<ScoringCriteria>
    {
        public void Configure(EntityTypeBuilder<ScoringCriteria> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .ValueGeneratedNever();

            builder.Property(x => x.AIPracticeMaterialId)
                   .IsRequired();

            builder.Property(x => x.Title)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.Description);

            builder.Property(x => x.Weight)
                   .HasPrecision(5, 2);

            builder.HasOne(x => x.AIPracticeMaterial)
                   .WithMany(x => x.ScoringCriterias)
                   .HasForeignKey(x => x.AIPracticeMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
