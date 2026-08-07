using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Constants
{
    public static class ReservedTagCodes
    {
        // Learner Type Tags
        public const string Student = "sinh-vien";
        public const string OfficeWorker = "nhan-vien-van-phong";
        public const string Freelancer = "freelancer";
        public const string BusinessOwner = "chu-doanh-nghiep";
        public const string CivilServant = "cong-chuc";
        public const string Retired = "nghi-huu";

        // Knowledge Level Tags
        public const string Beginner = "moi-bat-dau";
        public const string Intermediate = "co-ban";
        public const string Advanced = "nang-cao";

        public static readonly IReadOnlySet<string> LearnerTypeTags = new HashSet<string>
            {
                Student,
                OfficeWorker,
                Freelancer,
                BusinessOwner,
                CivilServant,
                Retired
            };
        public static readonly IReadOnlySet<string> LevelTags = new HashSet<string>
            {
                Beginner,
                Intermediate,
                Advanced
            };

        public static readonly IReadOnlySet<string> All =
            new HashSet<string>
            {
                Student,
                OfficeWorker,
                Freelancer,
                BusinessOwner,
                CivilServant,
                Retired,

                Beginner,
                Intermediate,
                Advanced
            };
    }
}
