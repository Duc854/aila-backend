using AILA.Domain.Constants;
using AILA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Seed
{
    public class SystemTagSeeder
    {
        private readonly ApplicationDbContext _context;

        public SystemTagSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            var existingCodes = (await _context.Tags
                .AsNoTracking()
                .Select(x => x.Code)
                .ToListAsync())
                .ToHashSet();


            var tags = new List<Tag>();

            AddIfMissing(
                tags,
                existingCodes,
                "Student",
                ReservedTagCodes.Student);


            AddIfMissing(
                tags,
                existingCodes,
                "Office Worker",
                ReservedTagCodes.OfficeWorker);


            AddIfMissing(
                tags,
                existingCodes,
                "Freelancer",
                ReservedTagCodes.Freelancer);


            AddIfMissing(
                tags,
                existingCodes,
                "Business Owner",
                ReservedTagCodes.BusinessOwner);


            AddIfMissing(
                tags,
                existingCodes,
                "Civil Servant",
                ReservedTagCodes.CivilServant);


            AddIfMissing(
                tags,
                existingCodes,
                "Retired",
                ReservedTagCodes.Retired);



            AddIfMissing(
                tags,
                existingCodes,
                "Beginner",
                ReservedTagCodes.Beginner);


            AddIfMissing(
                tags,
                existingCodes,
                "Intermediate",
                ReservedTagCodes.Intermediate);


            AddIfMissing(
                tags,
                existingCodes,
                "Advanced",
                ReservedTagCodes.Advanced);



            if (tags.Count == 0)
                return;


            await _context.Tags.AddRangeAsync(tags);

            await _context.SaveChangesAsync();
        }



        private static void AddIfMissing(
            List<Tag> tags,
            HashSet<string> existingCodes,
            string name,
            string code)
        {
            if (existingCodes.Contains(code))
                return;


            tags.Add(
                Tag.CreateByAdmin(
                    name,
                    code));
        }
    }
}
