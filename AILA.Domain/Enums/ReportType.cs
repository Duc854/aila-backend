using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Enums
{
    public enum ReportType
    {
        InappropriateContent = 1,
        HateSpeech = 2,
        Violence = 3,
        SexualContent = 4,
        Spam = 5,
        CopyrightViolation = 6,
        IncorrectInformation = 7,
        Other = 8
    }
}
