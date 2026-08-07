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
                "Sinh viên",
                ReservedTagCodes.Student);


            AddIfMissing(
                tags,
                existingCodes,
                "Nhân viên văn phòng",
                ReservedTagCodes.OfficeWorker);


            AddIfMissing(
                tags,
                existingCodes,
                "Freelancer",
                ReservedTagCodes.Freelancer);


            AddIfMissing(
                tags,
                existingCodes,
                "Chủ doanh nghiệp",
                ReservedTagCodes.BusinessOwner);


            AddIfMissing(
                tags,
                existingCodes,
                "Công chức",
                ReservedTagCodes.CivilServant);


            AddIfMissing(
                tags,
                existingCodes,
                "Nghỉ hưu",
                ReservedTagCodes.Retired);



            AddIfMissing(
                tags,
                existingCodes,
                "Mới bắt đầu",
                ReservedTagCodes.Beginner);


            AddIfMissing(
                tags,
                existingCodes,
                "Cơ bản",
                ReservedTagCodes.Intermediate);


            AddIfMissing(
                tags,
                existingCodes,
                "Nâng cao",
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
