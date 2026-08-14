using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
    {
        public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
        {
            builder.ToTable("KnowledgeDocuments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MaterialId)
                   .IsRequired();

            builder.Property(x => x.CourseId)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .IsRequired();

            builder.HasOne(x => x.Material)
                   .WithMany()
                   .HasForeignKey(x => x.MaterialId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Course)
                   .WithMany()
                   .HasForeignKey(x => x.CourseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Chunks)
                   .WithOne(x => x.KnowledgeDocument)
                   .HasForeignKey(x => x.KnowledgeDocumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.MaterialId)
                   .IsUnique();

            builder.HasIndex(x => x.CourseId);
        }
    }
}
