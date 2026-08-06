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
    public class AccountResourceLimitConfiguration
        : IEntityTypeConfiguration<AccountResourceLimit>
    {
        public void Configure(EntityTypeBuilder<AccountResourceLimit> builder)
        {
            builder.ToTable("AccountResourceLimits");


            // Primary Key

            builder.HasKey(x => x.Id);



            // Properties

            builder.Property(x => x.AccountId)
                .IsRequired();


            builder.Property(x => x.AiTokenLimit)
                .IsRequired(false);


            builder.Property(x => x.AiPracticeScenarioLimit)
                .IsRequired(false);


            builder.Property(x => x.ExpertEvaluationRequestLimit)
                .IsRequired(false);



            // Relationship
            // User 1 - 0..1 AccountResourceLimit

            builder.HasOne(x => x.Account)
                .WithOne()
                .HasForeignKey<AccountResourceLimit>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);



            // One account can have only one override

            builder.HasIndex(x => x.AccountId)
                .IsUnique();
        }
    }
}
