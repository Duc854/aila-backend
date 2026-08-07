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
        public const string Student = "student";
        public const string OfficeWorker = "office-worker";
        public const string Freelancer = "freelancer";
        public const string BusinessOwner = "business-owner";
        public const string CivilServant = "civil-servant";
        public const string Retired = "retired";

        // Knowledge Level Tags
        public const string Beginner = "beginner";
        public const string Intermediate = "intermediate";
        public const string Advanced = "advanced";


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
