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
    public class SubscriptionPlanConfiguration
        : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.Price)
                .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.HasIndex(x => x.TierLevel)
                .IsUnique();

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasMany(x => x.Subscriptions)
                .WithOne(x => x.SubscriptionPlan)
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(x => x.Subscriptions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.Payments)
                .WithOne(x => x.SubscriptionPlan)
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(x => x.Payments)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
