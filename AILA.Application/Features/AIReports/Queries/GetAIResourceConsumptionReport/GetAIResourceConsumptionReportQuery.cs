using AILA.Application.Features.AIReports.Dtos;
using MediatR;
using System;

namespace AILA.Application.Features.AIReports.Queries.GetAIResourceConsumptionReport;

public record GetAIResourceConsumptionReportQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<AIResourceConsumptionReportDto>;
