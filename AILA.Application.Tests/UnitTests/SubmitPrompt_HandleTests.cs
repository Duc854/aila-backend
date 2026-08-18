using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT28_SubmitPrompt — <see cref="SubmitPromptCommandHandler.Handle"/>
/// Module: AIPractice · CC = 11 · 12 test case
///
/// Nhánh: B1 = attempt null · B2 = material null · B3 = hết lượt submit
///        B4 = prompt không hợp lệ · B5 = là lỗi định dạng (không ghi DB)
///        B6 = nội dung không an toàn (moderation) · B7 = enrollment null
///        B8 = vượt hạn mức token · B9 = đạt max lượt ⇒ tự động chấm điểm &amp; hoàn thành
///
/// Đây là luồng AI lõi, nối 6 service: PrivacyService (che PII) → PromptValidationService
/// → ModerationService → QuotaService → PracticeChatService → ScoringService.
/// Điểm phân loại quan trọng ở B5: lỗi ĐỊNH DẠNG trả "ValidationError" và KHÔNG ghi DB,
/// còn vi phạm CHÍNH SÁCH trả "Violation" và PHẢI lưu lại để phục vụ kiểm duyệt.
///
/// LƯU Ý THAY ĐỔI MÔ HÌNH VI PHẠM: bản trước lưu vi phạm bằng "submission bị reject" ngay
/// trong aggregate PracticeAttempt (AddRejectedSubmission/IsRejected). Cơ chế đó ĐÃ BỊ GỠ —
/// vi phạm nay ghi ra bảng riêng <see cref="UserViolationRecord"/>, còn Submissions của attempt
/// chỉ chứa các lượt THÀNH CÔNG. Vì vậy các case vi phạm assert trên Repository&lt;UserViolationRecord&gt;
/// thay vì trên attempt.Submissions.
///
/// Bẫy phụ thuộc: nhánh vi phạm ghi DB chỉ khi tra được LearnerId từ enrollment
/// (accountIdForViolation != Guid.Empty) ⇒ mock Enrollments.GetByIdAsync phải trả non-null.
/// </summary>
public class UT28_SubmitPrompt_HandleTests
{
    private const string RawPrompt = "Hay viet mot email cam on khach hang da mua san pham";
    private const string AiResponse = "Chao ban, toi rat vui duoc ho tro.";
    private const string AiTask = "Dong vai khach hang";

    private readonly Mock<IPracticeAttemptRepository> _attemptRepo = new();
    private readonly Mock<IAIPracticeMaterialRepository> _materialRepo = new();
    private readonly Mock<IPromptValidationService> _promptValidation = new();
    private readonly Mock<IPracticeChatService> _chatService = new();
    private readonly Mock<IScoringService> _scoringService = new();
    private readonly Mock<IModerationService> _moderation = new();
    private readonly Mock<IPrivacyService> _privacy = new();
    private readonly Mock<IQuotaService> _quota = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEnrollmentRepository> _enrollments = new();
    private readonly Mock<ILearningProgressRepository> _progresses = new();
    private readonly Mock<IGenericRepository<PromptSubmission>> _submissionRepo = new();
    private readonly Mock<IGenericRepository<UserViolationRecord>> _violationRepo = new();
    private readonly Mock<IGenericRepository<AIFeedback>> _feedbackRepo = new();
    private readonly Mock<ILearnerBehaviorService> _learnerBehavior = new();

    private readonly Guid _attemptId = Guid.NewGuid();
    private readonly Guid _materialId = Guid.NewGuid();
    private readonly Guid _enrollmentId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();

    public UT28_SubmitPrompt_HandleTests()
    {
        _uow.SetupGet(x => x.Enrollments).Returns(_enrollments.Object);
        _uow.SetupGet(x => x.LearningProgresses).Returns(_progresses.Object);
        _uow.Setup(x => x.Repository<PromptSubmission>()).Returns(_submissionRepo.Object);
        _uow.Setup(x => x.Repository<UserViolationRecord>()).Returns(_violationRepo.Object);
        _uow.Setup(x => x.Repository<AIFeedback>()).Returns(_feedbackRepo.Object);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _progresses.Setup(x => x.GetByCompositeKeyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((LearningProgress?)null);

        // Mặc định: che PII trả lại nguyên văn, prompt hợp lệ, nội dung an toàn, còn hạn mức.
        _privacy.Setup(x => x.MaskSensitiveData(It.IsAny<string>())).Returns<string>(s => s);
        _promptValidation.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<PracticeAttempt>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((true, null, null));
        _moderation.Setup(x => x.CheckContentSafetyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((true, string.Empty));
        _quota.Setup(x => x.CheckQuotaAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new QuotaCheckResultDto { IsAllowed = true });
        _chatService.Setup(x => x.GetChatResponseAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatMessage>>(),
                        It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(AiResponse);
        _scoringService.Setup(x => x.GenerateOverallSuggestionAsync(
                           It.IsAny<List<PromptSubmission>>(), It.IsAny<string>(), It.IsAny<string>(),
                           It.IsAny<List<ScoringCriteria>>(), It.IsAny<string>(),
                           It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new OverallScoringResult { Percentage = 85m, Summary = "Lam tot" });
    }

    private SubmitPromptCommandHandler CreateSut() => new(
        _attemptRepo.Object, _materialRepo.Object, _promptValidation.Object, _chatService.Object,
        _scoringService.Object, _moderation.Object, _privacy.Object, _quota.Object, _uow.Object,
        _learnerBehavior.Object);

    private PracticeAttempt BuildAttempt(int previousSubmissions = 0)
    {
        var attempt = new PracticeAttempt(_enrollmentId, _materialId);
        for (var i = 0; i < previousSubmissions; i++)
            attempt.AddSubmission($"Prompt truoc do so {i + 1}", "AI response");
        return attempt;
    }

    private AIPracticeMaterial BuildMaterial(int maxPromptAttempts = 3) =>
        new(_materialId, "Kich ban ban hang", AiTask, "Thuyet phuc khach hang",
            PracticeDifficulty.Easy, maxPromptAttempts);

    private PracticeAttempt SetupScenario(PracticeAttempt? attempt, AIPracticeMaterial? material,
        Enrollment? enrollment = null)
    {
        _attemptRepo.Setup(x => x.GetByIdAsync(_attemptId, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        _materialRepo.Setup(x => x.GetByIdWithDetailsAsync(_materialId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(material);
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId))
                    .ReturnsAsync(enrollment ?? new Enrollment(_learnerId, Guid.NewGuid(), 3));
        return attempt!;
    }

    private void SetupInvalidPrompt(string reason, string policyName) =>
        _promptValidation.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<PracticeAttempt>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((false, reason, policyName));

    private Task<PromptSubmissionDto> Act() =>
        CreateSut().Handle(new SubmitPromptCommand(_attemptId, RawPrompt), CancellationToken.None);

    private void AssertNoAiCall() =>
        _chatService.Verify(x => x.GetChatResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatMessage>>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);

    private void AssertViolationRecorded(string policyName) =>
        _violationRepo.Verify(x => x.AddAsync(It.Is<UserViolationRecord>(v =>
            v.UserId == _learnerId && v.PolicyName == policyName)), Times.Once);

    private void AssertNoViolationRecorded() =>
        _violationRepo.Verify(x => x.AddAsync(It.IsAny<UserViolationRecord>()), Times.Never);

    /// <summary>UTCID01 · B1=T · Type A — không tìm thấy lượt thực hành.</summary>
    [Fact]
    public async Task UTCID01_AttemptNotFound_ThrowsNotFound()
    {
        SetupScenario(null, BuildMaterial());

        await Assert.ThrowsAsync<NotFoundException>(Act);
        AssertNoAiCall();
    }

    /// <summary>UTCID02 · B1=F, B2=T · Type A — không tìm thấy học liệu AI Practice.</summary>
    [Fact]
    public async Task UTCID02_MaterialNotFound_ThrowsNotFound()
    {
        SetupScenario(BuildAttempt(), null);

        await Assert.ThrowsAsync<NotFoundException>(Act);
        AssertNoAiCall();
    }

    /// <summary>UTCID03 · B3=T · Type A — đã dùng hết lượt submit (1/1).</summary>
    [Fact]
    public async Task UTCID03_MaxAttemptsReached_ThrowsBusinessRule()
    {
        SetupScenario(BuildAttempt(previousSubmissions: 1), BuildMaterial(maxPromptAttempts: 1));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(Act);

        Assert.Contains("Đã sử dụng hết 1/1 lượt submit", ex.Message);
        AssertNoAiCall();
    }

    /// <summary>
    /// UTCID04 · B4=T, B5=T · Type B — prompt quá ngắn (lỗi ĐỊNH DẠNG).
    /// KHÔNG ghi DB để tránh rác log kiểm duyệt.
    /// </summary>
    [Fact]
    public async Task UTCID04_FormatError_ReturnsValidationErrorWithoutPersisting()
    {
        var attempt = SetupScenario(BuildAttempt(), BuildMaterial());
        SetupInvalidPrompt("Prompt quá ngắn (cần ít nhất 5 ký tự).", "TooShortPrompt");

        var result = await Act();

        Assert.Equal("ValidationError", result.Status);
        Assert.False(result.IsViolation);
        Assert.Equal("Prompt quá ngắn (cần ít nhất 5 ký tự).", result.WarningMessage);
        Assert.Empty(attempt.Submissions);
        AssertNoViolationRecorded();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        AssertNoAiCall();
    }

    /// <summary>UTCID05 · B5=T · Type A — prompt rỗng, cũng là lỗi định dạng.</summary>
    [Fact]
    public async Task UTCID05_EmptyPromptPolicy_ReturnsValidationError()
    {
        var attempt = SetupScenario(BuildAttempt(), BuildMaterial());
        SetupInvalidPrompt("Nội dung prompt không được để trống.", "EmptyPrompt");

        var result = await Act();

        Assert.Equal("ValidationError", result.Status);
        Assert.Empty(attempt.Submissions);
        AssertNoViolationRecorded();
    }

    /// <summary>
    /// UTCID06 · B4=T, B5=F · Type A — vi phạm PII (vi phạm CHÍNH SÁCH).
    /// PHẢI ghi UserViolationRecord để phục vụ kiểm duyệt, nhưng KHÔNG tính vào lượt submit.
    /// </summary>
    [Fact]
    public async Task UTCID06_PiiViolation_PersistsViolationRecord()
    {
        var attempt = SetupScenario(BuildAttempt(), BuildMaterial());
        SetupInvalidPrompt("Phát hiện thông tin cá nhân: Email.", "PIIViolation");

        var result = await Act();

        Assert.Equal("Violation", result.Status);
        Assert.True(result.IsViolation);
        Assert.Equal("Phát hiện thông tin cá nhân: Email.", result.ViolationMessage);
        Assert.Empty(attempt.Submissions);
        AssertViolationRecorded("PIIViolation");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        AssertNoAiCall();
    }

    /// <summary>UTCID07 · B5=F · Type A — vượt rate limit cũng là vi phạm chính sách.</summary>
    [Fact]
    public async Task UTCID07_RateLimitViolation_PersistsViolationRecord()
    {
        SetupScenario(BuildAttempt(), BuildMaterial());
        SetupInvalidPrompt("Bạn đã gửi 20/20 prompt trong 1 phút. Vui lòng chậm lại.", "RateLimitExceeded");

        var result = await Act();

        Assert.Equal("Violation", result.Status);
        AssertViolationRecorded("RateLimitExceeded");
    }

    /// <summary>
    /// UTCID08 · B4=F, B5=F · Type A — vi phạm chính sách nhưng KHÔNG tra được enrollment
    /// ⇒ accountIdForViolation == Guid.Empty ⇒ BỎ QUA ghi DB nhưng vẫn trả "Violation".
    /// Nhánh guard này không case nào khác chạm tới.
    /// </summary>
    [Fact]
    public async Task UTCID08_ViolationWithoutEnrollment_SkipsPersistenceButStillReturnsViolation()
    {
        SetupScenario(BuildAttempt(), BuildMaterial());
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId)).ReturnsAsync((Enrollment?)null);
        SetupInvalidPrompt("Phát hiện thông tin cá nhân: Email.", "PIIViolation");

        var result = await Act();

        Assert.Equal("Violation", result.Status);
        Assert.True(result.IsViolation);
        AssertNoViolationRecorded();
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>UTCID09 · B6=T · Type A — nội dung không an toàn theo ModerationService.</summary>
    [Fact]
    public async Task UTCID09_UnsafeContent_PersistsContentModerationViolation()
    {
        var attempt = SetupScenario(BuildAttempt(), BuildMaterial());
        _moderation.Setup(x => x.CheckContentSafetyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((false, "Nội dung mang tính thù ghét."));

        var result = await Act();

        Assert.Equal("Violation", result.Status);
        Assert.True(result.IsViolation);
        Assert.Equal("Nội dung mang tính thù ghét.", result.ViolationMessage);
        Assert.Empty(attempt.Submissions);
        AssertViolationRecorded("ContentModeration");
        AssertNoAiCall();
    }

    /// <summary>
    /// UTCID10 · B7=T · Type A — không tìm thấy enrollment ở bước kiểm hạn mức.
    /// Khác UTCID08: prompt HỢP LỆ nên chạy tới bước 4.5 và ném NotFoundException.
    /// </summary>
    [Fact]
    public async Task UTCID10_EnrollmentNotFound_ThrowsNotFound()
    {
        SetupScenario(BuildAttempt(), BuildMaterial());
        _enrollments.Setup(x => x.GetByIdAsync(_enrollmentId)).ReturnsAsync((Enrollment?)null);

        await Assert.ThrowsAsync<NotFoundException>(Act);
        AssertNoAiCall();
    }

    /// <summary>UTCID11 · B8=T · Type A — hết hạn mức token trong kỳ.</summary>
    [Fact]
    public async Task UTCID11_QuotaExceeded_ReturnsQuotaExceededWithoutCallingAi()
    {
        var attempt = SetupScenario(BuildAttempt(), BuildMaterial());
        _quota.Setup(x => x.CheckQuotaAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new QuotaCheckResultDto { IsAllowed = false, WarningMessage = "Đã hết hạn mức Token." });

        var result = await Act();

        Assert.Equal("QuotaExceeded", result.Status);
        Assert.False(result.IsViolation);
        Assert.Equal("Đã hết hạn mức Token.", result.WarningMessage);
        Assert.Empty(attempt.Submissions);
        AssertNoAiCall();
    }

    /// <summary>
    /// UTCID12 · Toàn bộ nhánh = F, B9=F · Type N — submit thành công, chưa đạt max lượt.
    /// KHÔNG kích hoạt chấm điểm tổng thể.
    /// </summary>
    [Fact]
    public async Task UTCID12_HappyPath_CallsAiAndPersistsSubmission()
    {
        var attempt = SetupScenario(BuildAttempt(), BuildMaterial(maxPromptAttempts: 3));

        var result = await Act();

        Assert.Equal("Success", result.Status);
        Assert.False(result.IsViolation);
        Assert.Equal(RawPrompt, result.UserPrompt);
        Assert.Equal(AiResponse, result.AiResponse);
        Assert.Single(attempt.Submissions);
        Assert.Equal(PracticeAttemptStatus.InProgress, attempt.Status);

        _privacy.Verify(x => x.MaskSensitiveData(RawPrompt), Times.Once);
        _chatService.Verify(x => x.GetChatResponseAsync(
            AiTask, RawPrompt, It.IsAny<List<ChatMessage>>(),
            _attemptId, _learnerId, It.IsAny<CancellationToken>()), Times.Once);
        _submissionRepo.Verify(x => x.AddAsync(It.IsAny<PromptSubmission>()), Times.Once);
        _scoringService.Verify(x => x.GenerateOverallSuggestionAsync(
            It.IsAny<List<PromptSubmission>>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<ScoringCriteria>>(), It.IsAny<string>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// UTCID13 · B9=T · Type B — lượt submit cuối cùng (đạt MaxPromptAttempts = 1)
    /// ⇒ tự động chấm điểm tổng thể, chuyển attempt sang Completed, ghi AIFeedback
    /// và đánh dấu học liệu hoàn thành trong LearningProgress.
    /// </summary>
    [Fact]
    public async Task UTCID13_LastAllowedSubmission_AutoScoresAndCompletesAttempt()
    {
        var attempt = SetupScenario(BuildAttempt(previousSubmissions: 0), BuildMaterial(maxPromptAttempts: 1));

        var result = await Act();

        Assert.Equal("Success", result.Status);
        Assert.Equal(PracticeAttemptStatus.Completed, attempt.Status);
        Assert.Equal(85m, attempt.FinalScore);
        Assert.Equal("Lam tot", attempt.OverallSuggestion);
        _scoringService.Verify(x => x.GenerateOverallSuggestionAsync(
            It.IsAny<List<PromptSubmission>>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<ScoringCriteria>>(), It.IsAny<string>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        _feedbackRepo.Verify(x => x.AddAsync(It.Is<AIFeedback>(f => f.AttemptId == attempt.Id)), Times.Once);
        _progresses.Verify(x => x.AddAsync(It.IsAny<LearningProgress>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
