using AILA.Application.Common.Interfaces;
using AILA.Application.Features.AIReports.Dtos;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.AIReports.Queries.GetAIPolicyViolations;

public class GetAIPolicyViolationsQueryHandler : IRequestHandler<GetAIPolicyViolationsQuery, PaginatedViolationListDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAIPolicyViolationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedViolationListDto> Handle(GetAIPolicyViolationsQuery request, CancellationToken cancellationToken)
    {
        var records = await _unitOfWork.Repository<UserViolationRecord>().FindAsync(v =>
            (string.IsNullOrEmpty(request.ViolationType) || v.ViolationType.ToLower() == request.ViolationType.ToLower()) &&
            (string.IsNullOrEmpty(request.Severity) || v.Severity.ToLower() == request.Severity.ToLower()));

        var recordList = records.OrderByDescending(v => v.CreatedAt).ToList();
        var totalCount = recordList.Count;

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedItems = recordList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new AIPolicyViolationDto
            {
                Id = v.Id,
                UserId = v.UserId,
                AttemptId = v.AttemptId,
                ViolationType = v.ViolationType,
                PolicyName = v.PolicyName,
                Reason = v.Reason,
                Severity = v.Severity,
                CreatedAt = v.CreatedAt
            })
            .ToList();

        return new PaginatedViolationListDto
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            TotalPages = totalPages == 0 ? 1 : totalPages,
            TotalCount = totalCount
        };
    }
}
