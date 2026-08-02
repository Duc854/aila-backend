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
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.HasIndex(x => x.Status);

            builder.Property(x => x.ActivatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiredAt)
                .IsRequired();

            builder.ComplexProperty(x => x.PlanSnapshot, snapshot =>
            {
                snapshot.Property(x => x.TierLevel)
                    .HasColumnName("TierLevel")
                    .IsRequired();

                snapshot.Property(x => x.DurationInDays)
                    .HasColumnName("DurationInDays")
                    .IsRequired();

                snapshot.Property(x => x.AiTokenLimit)
                    .HasColumnName("AiTokenLimit")
                    .IsRequired();

                snapshot.Property(x => x.AiPracticeScenarioLimit)
                    .HasColumnName("AiPracticeScenarioLimit")
                    .IsRequired();

                snapshot.Property(x => x.ExpertEvaluationLimit)
                    .HasColumnName("ExpertEvaluationLimit")
                    .IsRequired();
            });

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(x => x.Learner)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.LearnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SubscriptionPlan)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Payment)
                .WithOne()
                .HasForeignKey<Subscription>(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
