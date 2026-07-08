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
    public class LearnerConfiguration : IEntityTypeConfiguration<Learner>
    {
        public void Configure(EntityTypeBuilder<Learner> builder)
        {
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.LearnerType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.KnowledgeLevel)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasMany(x => x.LearningGoals)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "LearnerLearningGoals",
                    right => right.HasOne<Tag>()
                        .WithMany()
                        .HasForeignKey("TagId"),
                    left => left.HasOne<Learner>()
                        .WithMany()
                        .HasForeignKey("LearnerId"));

            builder.Navigation(x => x.LearningGoals)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
