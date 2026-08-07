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
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.ThumbnailUrl)
                .HasMaxLength(512);

            builder.Property(x => x.Level)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.DurationHours)
                .HasPrecision(5, 2);

            builder.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Expert)
                .WithMany()
                .HasForeignKey(x => x.ExpertId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Modules)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Modules)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.CourseTags)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "CourseTags",
                    right => right.HasOne<Tag>()
                        .WithMany()
                        .HasForeignKey("TagId"),
                    left => left.HasOne<Course>()
                        .WithMany()
                        .HasForeignKey("CourseId"));

            builder.Navigation(x => x.CourseTags)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.ReviewRequests)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(x => x.ReviewRequests)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.ReviewRequests)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(x => x.ReviewRequests)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
