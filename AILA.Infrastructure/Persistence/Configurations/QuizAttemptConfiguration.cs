using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score)
            .HasPrecision(5, 2);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.SubmittedAt);

        builder.Property(x => x.IsPassed)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.EnrollmentId,
            x.QuizMaterialId
        });

        builder.HasOne(x => x.Enrollment)
            .WithMany()
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Answers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
