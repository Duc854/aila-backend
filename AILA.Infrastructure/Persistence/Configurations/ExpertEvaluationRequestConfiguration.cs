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
    public class ExpertEvaluationRequestConfiguration
        : IEntityTypeConfiguration<ExpertEvaluationRequest>
    {
        public void Configure(EntityTypeBuilder<ExpertEvaluationRequest> builder)
        {
            builder.ToTable("ExpertEvaluationRequests");

            builder.HasKey(x => x.Id);


            builder.Property(x => x.Status)
                .IsRequired();


            builder.Property(x => x.RequestedAt)
                .IsRequired();


            // Practice Attempt
            builder.HasOne(x => x.PracticeAttempt)
                .WithMany()
                .HasForeignKey(x => x.PracticeAttemptId)
                .OnDelete(DeleteBehavior.Cascade);


            // Learner
            builder.HasOne(x => x.Learner)
                .WithMany()
                .HasForeignKey(x => x.LearnerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Expert optional
            builder.HasOne(x => x.Expert)
                .WithMany()
                .HasForeignKey(x => x.ExpertId)
                .OnDelete(DeleteBehavior.Restrict);


            // One request -> one evaluation
            builder.HasOne(x => x.ExpertEvaluation)
                .WithOne(x => x.EvaluationRequest)
                .HasForeignKey<ExpertEvaluation>(
                    x => x.EvaluationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
