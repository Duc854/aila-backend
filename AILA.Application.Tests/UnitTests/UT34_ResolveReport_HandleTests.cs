using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Reports.Commands.ResolveReport;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT34_ResolveReport — <see cref="ResolveReportCommandHandler.Handle"/>
/// Module: Moderation · CC = 4 · 5 test case
///
/// Nhánh: B1 = ReportId rỗng · B2 = report null · B3 = Status != Pending (BR-04)
///
/// BR-04: chỉ báo cáo đang Pending mới được đánh dấu đã xử lý. Nhánh B3 vừa chặn double-submit
/// vừa bảo vệ ResolvedAt khỏi bị ghi đè khi Admin bấm nhầm lần hai.
/// </summary>
public class UT34_ResolveReport_HandleTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IContentReportRepository> _reports = new();
    private readonly Guid _reportId = Guid.NewGuid();

    public UT34_ResolveReport_HandleTests()
    {
        _uow.SetupGet(x => x.ContentReports).Returns(_reports.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ResolveReportCommandHandler CreateSut() => new(_uow.Object);

    private static ContentReport BuildReport() =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, ReportType.Spam, "Nội dung spam");

    private ContentReport SetupReport(ContentReport? report)
    {
        _reports.Setup(x => x.GetByIdAsync(_reportId)).ReturnsAsync(report);
        return report!;
    }

    private Task<Shared.Wrappers.ResponseDto<Features.Reports.Dtos.ResolveReportResponseDto>> Act(Guid? id = null) =>
        CreateSut().Handle(new ResolveReportCommand(id ?? _reportId), CancellationToken.None);

    /// <summary>UTCID01 · B1=T · Type A — ReportId rỗng, chặn trước khi chạm repository.</summary>
    [Fact]
    public async Task UTCID01_EmptyReportId_ReturnsInvalidReportId()
    {
        var result = await Act(Guid.Empty);

        Assert.False(result.Success);
        Assert.Equal("INVALID_REPORT_ID", result.ErrorCode);
        _reports.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID02 · B2=T · Type A — không tìm thấy báo cáo.</summary>
    [Fact]
    public async Task UTCID02_ReportNotFound_ReturnsReportNotFound()
    {
        SetupReport(null);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("REPORT_NOT_FOUND", result.ErrorCode);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID03 · B3=T · Type A — báo cáo đã được xử lý trước đó (BR-04).</summary>
    [Fact]
    public async Task UTCID03_AlreadyResolved_ReturnsAlreadyResolved()
    {
        var report = BuildReport();
        report.Resolve();
        var resolvedAt = report.ResolvedAt;
        SetupReport(report);

        var result = await Act();

        Assert.False(result.Success);
        Assert.Equal("ALREADY_RESOLVED", result.ErrorCode);
        Assert.Equal("Báo cáo đã được xử lý.", result.ErrorMessage);
        Assert.Equal(resolvedAt, report.ResolvedAt);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID04 · Toàn bộ nhánh = F · Type N — xử lý báo cáo thành công.</summary>
    [Fact]
    public async Task UTCID04_PendingReport_ResolvesAndSaves()
    {
        var report = SetupReport(BuildReport());

        var result = await Act();

        Assert.True(result.Success);
        Assert.Equal(report.Id, result.Data!.ReportId);
        Assert.Equal(nameof(ReportStatus.Resolved), result.Data.Status);
        Assert.Equal("Đã đánh dấu báo cáo là đã giải quyết.", result.Data.Message);
        Assert.NotEqual(default, result.Data.ResolvedAt);
        Assert.Equal(ReportStatus.Resolved, report.Status);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID05 · B3=T · Type B — Admin bấm xử lý hai lần liên tiếp.
    /// Lần hai phải bị chặn, ResolvedAt của lần đầu được giữ nguyên.
    /// </summary>
    [Fact]
    public async Task UTCID05_ResolvedTwice_SecondCallIsRejected()
    {
        var report = SetupReport(BuildReport());

        var first = await Act();
        var resolvedAt = report.ResolvedAt;
        var second = await Act();

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("ALREADY_RESOLVED", second.ErrorCode);
        Assert.Equal(resolvedAt, report.ResolvedAt);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
