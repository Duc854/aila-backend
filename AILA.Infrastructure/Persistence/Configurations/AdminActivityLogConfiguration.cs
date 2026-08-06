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
    public class AdminActivityLogConfiguration
        : IEntityTypeConfiguration<AdminActivityLog>
    {
        public void Configure(EntityTypeBuilder<AdminActivityLog> builder)
        {
            builder.ToTable("AdminActivityLogs");

            builder.HasKey(x => x.Id);


            builder.Property(x => x.Action)
                .HasConversion<string>()
                .IsRequired();


            builder.Property(x => x.EntityType)
                .HasMaxLength(100)
                .IsRequired();


            builder.Property(x => x.Description)
                .HasMaxLength(2000);


            builder.Property(x => x.IpAddress)
                .HasMaxLength(45);


            builder.HasOne(x => x.Admin)
                .WithMany()
                .HasForeignKey(x => x.AdminId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.HasIndex(x => new
            {
                x.AdminId,
                x.CreatedAt
            });
        }
    }
}
