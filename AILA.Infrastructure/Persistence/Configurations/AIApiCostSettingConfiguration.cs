using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AILA.Infrastructure.Persistence.Configurations
{
    public class AIApiCostSettingConfiguration : IEntityTypeConfiguration<AIApiCostSetting>
    {
        public void Configure(EntityTypeBuilder<AIApiCostSetting> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ModelId)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.ServiceName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.CostPerInputToken)
                   .HasPrecision(18, 8);

            builder.Property(x => x.CostPerOutputToken)
                   .HasPrecision(18, 8);

            builder.Property(x => x.Currency)
                   .HasMaxLength(10)
                   .HasDefaultValue("USD");
        }
    }
}
