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
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(x => x.Code)
                   .IsUnique();

            builder.Property(x => x.IsPublished)
                   .IsRequired();

            builder.Property(x => x.CreatedById)
                   .IsRequired(false);

            builder.HasOne(x => x.PublishRequest)
                   .WithOne(x => x.Tag)
                   .HasForeignKey<TagPublishRequest>(x => x.TagId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
