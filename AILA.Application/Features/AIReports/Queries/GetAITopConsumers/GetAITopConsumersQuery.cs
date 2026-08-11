using AILA.Application.Features.AIReports.Dtos;
using MediatR;
using System;

namespace AILA.Application.Features.AIReports.Queries.GetAITopConsumers;

public record GetAITopConsumersQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Top = 5
) : IRequest<AITopConsumersResponseDto>;
