using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class UserViolationRecordConfiguration : IEntityTypeConfiguration<UserViolationRecord>
    {
        public void Configure(EntityTypeBuilder<UserViolationRecord> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                   .IsRequired();

            builder.Property(x => x.ViolationType)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.PolicyName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Reason)
                   .HasMaxLength(1000);

            builder.Property(x => x.ViolatingPrompt)
                   .IsRequired();

            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
