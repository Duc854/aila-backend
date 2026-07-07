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
    public class TagPublishRequestConfiguration
        : IEntityTypeConfiguration<TagPublishRequest>
    {
        public void Configure(EntityTypeBuilder<TagPublishRequest> builder)
        {

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Note)
                   .HasMaxLength(1000);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(x => x.ReviewedAt);

            builder.Property(x => x.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasIndex(x => x.TagId)
                   .IsUnique();
        }
    }
}
