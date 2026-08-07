using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Seed
{
    public class ResourceLimitPolicySeeder
    {
        private readonly ApplicationDbContext _context;


        public ResourceLimitPolicySeeder(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task SeedAsync()
        {
            bool exists = await _context.ResourceLimitPolicies
                .AnyAsync();


            if (exists)
                return;


            var policies = new List<ResourceLimitPolicy>
            {
                new ResourceLimitPolicy(
                    ResourceAccountType.Learner,
                    aiTokenLimit: 100000,
                    aiPracticeScenarioLimit: 3,
                    expertEvaluationRequestLimit: 0
                ),

                new ResourceLimitPolicy(
                    ResourceAccountType.Expert,
                    aiTokenLimit: 500000,
                    aiPracticeScenarioLimit: 20,
                    expertEvaluationRequestLimit: 0
                )
            };


            await _context.ResourceLimitPolicies
                .AddRangeAsync(policies);


            await _context.SaveChangesAsync();
        }
    }
}
