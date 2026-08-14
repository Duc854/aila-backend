using AILA.Application.Features.AIReports.Dtos;
using MediatR;
using Shared.Wrappers;
using System;

namespace AILA.Application.Features.AIReports.Queries.GetAIServiceBreakdown;

public record GetAIServiceBreakdownQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<ResponseDto<AIServiceBreakdownResponseDto>>;
