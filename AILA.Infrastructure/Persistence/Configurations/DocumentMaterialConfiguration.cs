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
    public class DocumentMaterialConfiguration : IEntityTypeConfiguration<DocumentMaterial>
    {
        public void Configure(EntityTypeBuilder<DocumentMaterial> builder)
        {
            builder.HasKey(x => x.MaterialId);
        }
    }
}
