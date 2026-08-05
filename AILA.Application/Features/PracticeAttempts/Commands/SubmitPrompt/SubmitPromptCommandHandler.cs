using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;

namespace AILA.Application.Features.PracticeAttempts.Commands.SubmitPrompt;

public class SubmitPromptCommandHandler : IRequestHandler<SubmitPromptCommand, PromptSubmissionDto>
{
    private readonly IPracticeAttemptRepository _attemptRepo;
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IPromptValidationService _promptValidationService;
    private readonly IPracticeChatService _chatService;
    private readonly IScoringService _scoringService;
    private readonly IModerationService _moderationService;
    private readonly IPrivacyService _privacyService;
    private readonly IQuotaService _quotaService;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitPromptCommandHandler(
        IPracticeAttemptRepository attemptRepo,
        IAIPracticeMaterialRepository materialRepo,
        IPromptValidationService promptValidationService,
        IPracticeChatService chatService,
        IScoringService scoringService,
        IModerationService moderationService,
        IPrivacyService privacyService,
        IQuotaService quotaService,
        IUnitOfWork unitOfWork)
    {
        _attemptRepo = attemptRepo;
        _materialRepo = materialRepo;
        _promptValidationService = promptValidationService;
        _chatService = chatService;
        _scoringService = scoringService;
        _moderationService = moderationService;
        _privacyService = privacyService;
        _quotaService = quotaService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PromptSubmissionDto> Handle(SubmitPromptCommand request, CancellationToken cancellationToken)
    {
        // 1. Load attempt + material
        var attempt = await _attemptRepo.GetByIdAsync(request.AttemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(PracticeAttempt), request.AttemptId);

        var material = await _materialRepo.GetByIdAsync(attempt.MaterialId)
            ?? throw new NotFoundException(nameof(AIPracticeMaterial), attempt.MaterialId);

        // 2. Guard: Max prompt attempts (chỉ tính số lượt submit THÀNH CÔNG có AI Response)
        int validCount = attempt.Submissions.Count(s => !s.IsRejected);
        if (!attempt.CanSubmitMore(material.MaxPromptAttempts))
        {
            throw new BusinessRuleException(
                $"Đã sử dụng hết {validCount}/{material.MaxPromptAttempts} lượt submit thành công cho bài thực hành này. Vui lòng tạo attempt mới để tiếp tục.");
        }

        // Mask sensitive data (PII: Phone, Email, CCCD, Address) immediately
        var sanitizedPrompt = _privacyService.MaskSensitiveData(request.UserPrompt);

        // 3. Prompt Validation Check
        var (isValid, validationReason, policyName) = await _promptValidationService.ValidateAsync(
            request.UserPrompt, attempt, cancellationToken);

        if (!isValid)
        {
            // Lỗi nhập liệu định dạng (rỗng, quá ngắn, rác) -> ValidationError (Không rác DB log)
            bool isFormatError = policyName is "EmptyPrompt" or "TooShortPrompt" or "InvalidFormatPrompt" or "TooManySpecialChars";

            if (isFormatError)
            {
                return new PromptSubmissionDto
                {
                    Id = Guid.NewGuid(),
                    UserPrompt = sanitizedPrompt,
                    AiResponse = string.Empty,
                    Status = "ValidationError",
                    IsViolation = false,
                    WarningMessage = validationReason,
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Vi phạm chính sách/bảo mật (PII, Rate limit, Duplicate spam) -> Violation (lưu trực tiếp PolicyName vào Submission)
            var rejectedSubmission = attempt.AddRejectedSubmission(
                sanitizedPrompt, 
                validationReason ?? "Prompt vi phạm quy định", 
                policyName ?? "PromptValidation");

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PromptSubmissionDto
            {
                Id = rejectedSubmission.Id,
                UserPrompt = sanitizedPrompt,
                AiResponse = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = validationReason,
                WarningMessage = validationReason,
                CreatedAt = rejectedSubmission.CreatedAt
            };
        }

        // 4. Content Moderation Check (Kiểm tra độc hại/vi phạm an toàn)
        var (isSafe, moderationReason) = await _moderationService.CheckContentSafetyAsync(sanitizedPrompt, cancellationToken);
        if (!isSafe)
        {
            var rejectedSubmission = attempt.AddRejectedSubmission(
                sanitizedPrompt, 
                moderationReason ?? "Vi phạm quy chuẩn an toàn nội dung", 
                "ContentModeration");

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PromptSubmissionDto
            {
                Id = rejectedSubmission.Id,
                UserPrompt = sanitizedPrompt,
                AiResponse = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = moderationReason,
                WarningMessage = moderationReason,
                CreatedAt = rejectedSubmission.CreatedAt
            };
        }

        // 4.5. Check Quota Limit (Kiểm tra hạn mức Token trong ngày của User)
        var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(attempt.EnrollmentId)
            ?? throw new NotFoundException(nameof(Enrollment), attempt.EnrollmentId);
        var accountId = enrollment.LearnerId;
        var quotaResult = await _quotaService.CheckQuotaAsync(accountId, 1000, 0.80f, cancellationToken);
        if (!quotaResult.IsAllowed)
        {
            return new PromptSubmissionDto
            {
                Id = Guid.NewGuid(),
                UserPrompt = sanitizedPrompt,
                AiResponse = string.Empty,
                Status = "QuotaExceeded",
                IsViolation = false,
                WarningMessage = quotaResult.WarningMessage,
                CreatedAt = DateTime.UtcNow
            };
        }

        // 5. Build conversation history TRƯỚC KHI tạo submission mới
        // (tránh intermediate SaveChangesAsync từ QuotaService/ChatService flush submission chưa hoàn chỉnh)
        var systemPrompt = material.AITask;
        var history = attempt.Submissions
            .Where(s => !s.IsRejected)
            .OrderBy(s => s.CreatedAt)
            .SelectMany(s => new[] {
                new ChatMessage("user", s.UserPrompt),
                new ChatMessage("assistant", s.AiResponse)
            })
            .ToList();

        // 6. Gọi AI Customer Chat Service (gộp PlatformSystemPrompt + AITask)
        var aiResponse = await _chatService.GetChatResponseAsync(
            systemPrompt,
            sanitizedPrompt,
            history,
            attemptId: request.AttemptId,
            accountId: accountId,
            cancellationToken: cancellationToken);

        // 7. Mask AI response
        var sanitizedAiResponse = _privacyService.MaskSensitiveData(aiResponse);

        // 8. Khởi tạo submission thông qua DDD Aggregate Root method (PracticeAttempt)
        var submission = attempt.AddSubmission(sanitizedPrompt, sanitizedAiResponse);

        // 9. Check MaxPromptAttempts -> Auto-Complete if reaching max valid attempts
        var validSubmissionsCount = attempt.Submissions.Count(s => !s.IsRejected);
        if (validSubmissionsCount >= material.MaxPromptAttempts && attempt.Status == PracticeAttemptStatus.InProgress)
        {
            await ScoreAndCompleteAttemptAsync(attempt, material, accountId, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 10. Trả về DTO lượt chat thành công
        return new PromptSubmissionDto
        {
            Id = submission.Id,
            UserPrompt = submission.UserPrompt,
            AiResponse = submission.AiResponse,
            Status = "Success",
            IsViolation = false,
            ViolationMessage = null,
            WarningMessage = quotaResult.WarningMessage,
            CreatedAt = submission.CreatedAt
        };
    }

    private async Task ScoreAndCompleteAttemptAsync(
        PracticeAttempt attempt,
        AIPracticeMaterial material,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var validSubmissions = attempt.Submissions
            .Where(s => !s.IsRejected)
            .OrderBy(s => s.CreatedAt)
            .ToList();

        var scoringResult = await _scoringService.GenerateOverallSuggestionAsync(
            validSubmissions,
            material.Scenario,
            material.LearnerTask,
            material.ScoringCriterias.ToList(),
            material.AITask,
            attemptId: attempt.Id,
            accountId: accountId,
            cancellationToken: cancellationToken);

        attempt.Complete(scoringResult.Percentage, scoringResult.Summary);
    }
}
