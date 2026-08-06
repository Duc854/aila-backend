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
    public class ExpertEvaluationConfiguration
        : IEntityTypeConfiguration<ExpertEvaluation>
    {
        public void Configure(EntityTypeBuilder<ExpertEvaluation> builder)
        {
            builder.ToTable("ExpertEvaluations");

            builder.HasKey(x => x.Id);


            builder.Property(x => x.OverallScore)
                .HasPrecision(5, 2)
                .IsRequired();


            builder.Property(x => x.Feedback)
                .HasMaxLength(2000)
                .IsRequired();


            builder.Property(x => x.Recommendation)
                .HasMaxLength(1000)
                .IsRequired();


            builder.Property(x => x.EvaluatedAt)
                .IsRequired();


            builder.HasIndex(x => x.EvaluationRequestId)
                .IsUnique();
        }
    }
}
