// AILA.Application.Common.Interfaces.AI/IRoleParserService.cs
using AILA.Application.Common.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Common.Interfaces.AI;

public interface IRoleParserService
{
    Task<RoleParseResultDto> ParseRolesAsync(string aiTask, CancellationToken cancellationToken = default);
}
