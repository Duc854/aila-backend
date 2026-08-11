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
            string.IsNullOrEmpty(request.ViolationType) || v.ViolationType.ToLower() == request.ViolationType.ToLower());

        var recordList = records.OrderByDescending(v => v.CreatedAt).ToList();
        var totalCount = recordList.Count;

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var userIds = recordList.Select(v => v.UserId).Distinct().ToList();
        var users = (await _unitOfWork.Repository<User>().FindAsync(u => userIds.Contains(u.Id)))
            .ToDictionary(u => u.Id);

        var pagedItems = recordList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(v =>
            {
                users.TryGetValue(v.UserId, out var user);
                return new AIPolicyViolationDto
                {
                    Id = v.Id,
                    UserId = v.UserId,
                    FullName = user?.FullName ?? "Người dùng",
                    Email = user?.Email ?? "N/A",
                    ViolationType = v.ViolationType,
                    PolicyName = v.PolicyName,
                    Reason = v.Reason,
                    ViolatingPrompt = v.ViolatingPrompt,
                    CreatedAt = v.CreatedAt
                };
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
