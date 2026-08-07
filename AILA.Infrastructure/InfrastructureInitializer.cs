using AILA.Infrastructure.Persistence;
using AILA.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure
{
    public static class InfrastructureInitializer
    {
        public static async Task InitializeDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var logger = provider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                var dbContext = provider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();

                var adminSeeder = provider.GetRequiredService<AdminSeeder>();
                await adminSeeder.SeedAsync();
                var resourceLimitPolicySeeder = provider.GetRequiredService<ResourceLimitPolicySeeder>();
                await resourceLimitPolicySeeder.SeedAsync();
                var systemTagSeeder = provider.GetRequiredService<SystemTagSeeder>();
                await systemTagSeeder.SeedAsync();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Đã xảy ra lỗi khi migrate hoặc seed dữ liệu.");
                throw;
            }
        }
    }
}
