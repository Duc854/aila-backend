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
    public class LearnerTagScoreConfiguration
        : IEntityTypeConfiguration<LearnerTagScore>
    {
        public void Configure(EntityTypeBuilder<LearnerTagScore> builder)
        {
            builder.ToTable("LearnerTagScores");

            builder.HasKey(x => x.Id);


            builder.Property(x => x.ProfileSeed)
                .IsRequired();


            builder.Property(x => x.BehaviorScore)
                .IsRequired();


            // Learner 1 - N LearnerTagScore
            builder.HasOne(x => x.Learner)
                .WithMany(x => x.TagScores)
                .HasForeignKey(x => x.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);


            // Tag 1 - N LearnerTagScore
            builder.HasOne(x => x.Tag)
                .WithMany(x => x.LearnerScores)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);


            // Một learner chỉ có một score trên một tag
            builder.HasIndex(x => new
            {
                x.LearnerId,
                x.TagId
            })
            .IsUnique();
        }
    }
}
