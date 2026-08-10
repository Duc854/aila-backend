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
    public class AccountResourceUsageConfiguration
        : IEntityTypeConfiguration<AccountResourceUsage>
    {
        public void Configure(EntityTypeBuilder<AccountResourceUsage> builder)
        {
            builder.ToTable("AccountResourceUsages");


            // Primary Key

            builder.HasKey(x => x.Id);



            // Properties

            builder.Property(x => x.AccountId)
                .IsRequired();


            builder.Property(x => x.PeriodStart)
                .IsRequired();


            builder.Property(x => x.PeriodEnd)
                .IsRequired();



            builder.Property(x => x.AiTokenUsed)
                .IsRequired();


            builder.Property(x => x.AiPracticeScenarioUsed)
                .IsRequired();


            builder.Property(x => x.ExpertEvaluationRequestUsed)
                .IsRequired();



            // Relationship

            builder.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);



            // One account cannot have duplicate usage period

            builder.HasIndex(x => new
            {
                x.AccountId,
                x.PeriodStart,
                x.PeriodEnd
            })
            .IsUnique();
        }
    }
}
