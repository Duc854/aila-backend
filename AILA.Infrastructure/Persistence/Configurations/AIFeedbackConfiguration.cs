using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class AIFeedbackConfiguration : IEntityTypeConfiguration<AIFeedback>
    {
        public void Configure(EntityTypeBuilder<AIFeedback> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttemptId)
                   .IsRequired();

            builder.Property(x => x.FinalScore)
                   .HasPrecision(5, 2);

            builder.HasOne(x => x.Attempt)
                   .WithMany()
                   .HasForeignKey(x => x.AttemptId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
