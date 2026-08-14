using AILA.Application.Features.AIReports.Dtos;
using MediatR;
using Shared.Wrappers;
using System;

namespace AILA.Application.Features.AIReports.Queries.GetAIResourceConsumptionReport;

public record GetAIResourceConsumptionReportQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<ResponseDto<AIResourceConsumptionReportDto>>;
