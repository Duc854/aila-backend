using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations;

public class ExpertSimulationAttemptConfiguration : IEntityTypeConfiguration<ExpertSimulationAttempt>
{
    public void Configure(EntityTypeBuilder<ExpertSimulationAttempt> builder)
    {
        builder.ToTable("ExpertSimulationAttempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExpertId)
            .IsRequired();

        builder.Property(x => x.MaterialId)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.FinalScore)
            .HasPrecision(5, 2)
            .IsRequired(false);

        builder.Property(x => x.OverallSuggestion)
            .HasMaxLength(4000);

        builder.HasOne(x => x.Expert)
            .WithMany()
            .HasForeignKey(x => x.ExpertId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
