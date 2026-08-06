using AILA.Application.Common.Dtos.AI;
// Application/Features/PracticeAttempts/Queries/GetViolations/GetViolationsQuery.cs
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.PracticeAttempts.Queries.GetViolations;

public record GetViolationsQuery(Guid AttemptId) : IRequest<List<PromptViolationLogDto>>;
