using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
    {
        public void Configure(EntityTypeBuilder<KnowledgeChunk> builder)
        {
            builder.ToTable("KnowledgeChunks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Content)
                   .IsRequired();

            builder.Property(x => x.Embedding)
                   .IsRequired();

            builder.HasOne(x => x.KnowledgeDocument)
                   .WithMany(x => x.Chunks)
                   .HasForeignKey(x => x.KnowledgeDocumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.KnowledgeDocumentId);

            builder.HasIndex(x => x.CourseId);

            builder.HasIndex(x => new { x.KnowledgeDocumentId, x.ChunkIndex });
        }
    }
}
