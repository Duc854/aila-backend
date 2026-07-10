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
    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new
            {
                x.QuizMaterialId,
                x.OrderIndex
            }).IsUnique();

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.QuestionType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasMany(x => x.AnswerOptions)
                .WithOne(x => x.Question)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.AnswerOptions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
