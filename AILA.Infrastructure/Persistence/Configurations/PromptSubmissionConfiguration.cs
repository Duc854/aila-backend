using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class PromptSubmissionConfiguration : IEntityTypeConfiguration<PromptSubmission>
    {
        public void Configure(EntityTypeBuilder<PromptSubmission> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .ValueGeneratedNever();

            builder.Property(x => x.AttemptId)
                   .IsRequired();

            builder.Property(x => x.UserPrompt)
                   .IsRequired();

            builder.HasOne(x => x.Attempt)
                   .WithMany(a => a.Submissions)
                   .HasForeignKey(x => x.AttemptId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
