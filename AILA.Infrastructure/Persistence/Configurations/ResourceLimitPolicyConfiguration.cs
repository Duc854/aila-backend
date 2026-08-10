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
    public class ResourceLimitPolicyConfiguration : IEntityTypeConfiguration<ResourceLimitPolicy>
    {
        public void Configure(EntityTypeBuilder<ResourceLimitPolicy> builder)
        {
            builder.ToTable("ResourceLimitPolicies");

            // Primary Key
            builder.HasKey(x => x.Id);


            // Properties

            builder.Property(x => x.AccountType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.AiTokenLimit)
                .IsRequired();

            builder.Property(x => x.AiPracticeScenarioLimit)
                .IsRequired();

            builder.Property(x => x.ExpertEvaluationRequestLimit)
                .IsRequired();


            // Audit fields inherited from BaseEntity
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);


            // Business Constraint:
            // Mỗi loại resource account chỉ có một default policy
            builder.HasIndex(x => x.AccountType)
                .IsUnique();
        }
    }
}
