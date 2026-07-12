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
    public class VideoMaterialConfiguration : IEntityTypeConfiguration<VideoMaterial>
    {
        public void Configure(EntityTypeBuilder<VideoMaterial> builder)
        {
            builder.HasKey(x => x.MaterialId);

            builder.Property(x => x.VideoUrl)
                .IsRequired()
                .HasMaxLength(512);

        }
    }
}
