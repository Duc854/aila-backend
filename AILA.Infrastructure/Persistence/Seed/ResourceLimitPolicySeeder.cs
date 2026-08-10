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
            var hasLearner = await _context.ResourceLimitPolicies
                .AnyAsync(p => p.AccountType == ResourceAccountType.Learner);

            if (!hasLearner)
            {
                await _context.ResourceLimitPolicies.AddAsync(new ResourceLimitPolicy(
                    ResourceAccountType.Learner,
                    aiTokenLimit: 100000,
                    aiPracticeScenarioLimit: 3,
                    expertEvaluationRequestLimit: 0
                ));
            }

            var hasExpert = await _context.ResourceLimitPolicies
                .AnyAsync(p => p.AccountType == ResourceAccountType.Expert);

            if (!hasExpert)
            {
                await _context.ResourceLimitPolicies.AddAsync(new ResourceLimitPolicy(
                    ResourceAccountType.Expert,
                    aiTokenLimit: 500000,
                    aiPracticeScenarioLimit: 20,
                    expertEvaluationRequestLimit: 0
                ));
            }

            if (!hasLearner || !hasExpert)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
