using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class CourseChatSessionConfiguration : IEntityTypeConfiguration<CourseChatSession>
    {
        public void Configure(EntityTypeBuilder<CourseChatSession> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AccountId)
                   .IsRequired();

            builder.Property(x => x.CourseId)
                   .IsRequired();

            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.HasOne(x => x.Account)
                   .WithMany()
                   .HasForeignKey(x => x.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Course)
                   .WithMany()
                   .HasForeignKey(x => x.CourseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Messages)
                   .WithOne(m => m.Session)
                   .HasForeignKey(m => m.SessionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
