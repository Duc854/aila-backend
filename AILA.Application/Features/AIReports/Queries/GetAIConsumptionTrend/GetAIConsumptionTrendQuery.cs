using AILA.Application.Features.AIReports.Dtos;
using MediatR;
using System;

namespace AILA.Application.Features.AIReports.Queries.GetAIConsumptionTrend;

public record GetAIConsumptionTrendQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string Interval = "day"
) : IRequest<AIConsumptionTrendResponseDto>;
