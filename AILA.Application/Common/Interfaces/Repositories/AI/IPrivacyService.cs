using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.Repositories.AI
{
    public interface IPrivacyService
    {
        Task<string> MaskAsync(string rawText, CancellationToken ct);
    }
}
