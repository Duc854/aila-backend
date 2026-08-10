using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class AITokenLogConfiguration : IEntityTypeConfiguration<AITokenLog>
    {
        public void Configure(EntityTypeBuilder<AITokenLog> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AccountId)
                   .IsRequired();

            builder.Property(x => x.ServiceType)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.ModelId)
                   .HasMaxLength(100);

            builder.HasOne(x => x.Account)
                   .WithMany()
                   .HasForeignKey(x => x.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.AttemptId);
        }
    }
}
