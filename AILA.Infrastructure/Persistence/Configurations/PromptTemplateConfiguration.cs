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
    public class PromptTemplateConfiguration : IEntityTypeConfiguration<PromptTemplate>
    {
        public void Configure(EntityTypeBuilder<PromptTemplate> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .ValueGeneratedNever();

            builder.Property(x => x.AIPracticeMaterialId)
                   .IsRequired();

            builder.Property(x => x.Title)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Content)
                   .IsRequired();

            builder.HasOne(x => x.AIPracticeMaterial)
                   .WithMany(x => x.PromptTemplates)
                   .HasForeignKey(x => x.AIPracticeMaterialId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
