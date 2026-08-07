using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Application.Features.ExpertSimulations.Commands.SubmitSimulationPrompt;

public class SubmitSimulationPromptCommandHandler : IRequestHandler<SubmitSimulationPromptCommand, PromptSubmissionDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIPracticeMaterialRepository _materialRepo;
    private readonly IPrivacyService _privacyService;
    private readonly IModerationService _moderationService;
    private readonly IQuotaService _quotaService;

    public SubmitSimulationPromptCommandHandler(
        IUnitOfWork unitOfWork,
        IAIPracticeMaterialRepository materialRepo,
        IPrivacyService privacyService,
        IModerationService moderationService,
        IQuotaService quotaService)
    {
        _unitOfWork = unitOfWork;
        _materialRepo = materialRepo;
        _privacyService = privacyService;
        _moderationService = moderationService;
        _quotaService = quotaService;
    }

    public async Task<PromptSubmissionDto> Handle(SubmitSimulationPromptCommand request, CancellationToken cancellationToken)
    {
        // 1. Get ExpertSimulationAttempt
        var attempt = await _unitOfWork.Repository<ExpertSimulationAttempt>().GetByIdAsync(request.SimulationAttemptId)
            ?? throw new NotFoundException(nameof(ExpertSimulationAttempt), request.SimulationAttemptId);

        var material = await _materialRepo.GetByIdAsync(attempt.MaterialId)
            ?? throw new NotFoundException("AIPracticeMaterial", attempt.MaterialId);

        // 2. Check maximum prompt attempts
        if (!attempt.CanSubmitMore(material.MaxPromptAttempts))
        {
            throw new InvalidOperationException($"Simulation đã đạt giới hạn tối đa ({material.MaxPromptAttempts} lượt) hoặc đã kết thúc.");
        }

        // 3. PII & Basic Prompt Validation (BR-07)
        if (string.IsNullOrWhiteSpace(request.UserPrompt) || request.UserPrompt.Length < 5)
        {
            return new PromptSubmissionDto
            {
                Id = Guid.NewGuid(),
                UserPrompt = request.UserPrompt,
                AiResponse = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = "Prompt quá ngắn hoặc rỗng.",
                WarningMessage = "Prompt quá ngắn hoặc rỗng.",
                CreatedAt = DateTime.UtcNow
            };
        }

        if (_privacyService.HasSensitiveData(request.UserPrompt))
        {
            var piiTypes = _privacyService.GetSensitiveDataTypes(request.UserPrompt);
            var validationReason = $"Phát hiện thông tin cá nhân: {string.Join(", ", piiTypes)}.";

            var rejectedSubmission = attempt.AddRejectedSubmission(
                request.UserPrompt,
                validationReason,
                "PIIProtection");

            var violationRecord = new UserViolationRecord(
                attempt.ExpertId,
                "PromptValidationViolation",
                "PIIProtection",
                validationReason,
                attemptId: attempt.Id,
                severity: "Medium");
            await _unitOfWork.Repository<UserViolationRecord>().AddAsync(violationRecord);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PromptSubmissionDto
            {
                Id = rejectedSubmission.Id,
                UserPrompt = request.UserPrompt,
                AiResponse = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = validationReason,
                WarningMessage = validationReason,
                CreatedAt = rejectedSubmission.CreatedAt
            };
        }

        // 4. Content Moderation Check
        var (isSafe, moderationReason) = await _moderationService.CheckContentSafetyAsync(request.UserPrompt, cancellationToken);
        if (!isSafe)
        {
            var rejectedSubmission = attempt.AddRejectedSubmission(
                request.UserPrompt,
                moderationReason ?? "Vi phạm quy chuẩn an toàn nội dung",
                "ContentModeration");

            var violationRecord = new UserViolationRecord(
                attempt.ExpertId,
                "ContentModerationViolation",
                "ContentModeration",
                moderationReason ?? "Vi phạm quy chuẩn an toàn nội dung",
                attemptId: attempt.Id,
                severity: "High");
            await _unitOfWork.Repository<UserViolationRecord>().AddAsync(violationRecord);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PromptSubmissionDto
            {
                Id = rejectedSubmission.Id,
                UserPrompt = request.UserPrompt,
                AiResponse = string.Empty,
                Status = "Violation",
                IsViolation = true,
                ViolationMessage = moderationReason,
                WarningMessage = moderationReason,
                CreatedAt = rejectedSubmission.CreatedAt
            };
        }

        // 5. Check Quota Limit (BR-01)
        var quotaResult = await _quotaService.CheckQuotaAsync(attempt.ExpertId, 1000, 0.80f, cancellationToken);
        if (!quotaResult.IsAllowed)
        {
            return new PromptSubmissionDto
            {
                Id = Guid.NewGuid(),
                UserPrompt = request.UserPrompt,
                AiResponse = string.Empty,
                Status = "QuotaExceeded",
                IsViolation = false,
                WarningMessage = "Hạn mức Token AI của Expert không đủ để tiếp tục simulation.",
                CreatedAt = DateTime.UtcNow
            };
        }

        // Mock simulation AI response for testing / execution pipeline
        var mockAiResponse = $"[Simulation Persona Response] Cảm ơn bạn. Câu trả lời thử nghiệm của Expert: '{request.UserPrompt}'";
        var submission = attempt.AddSubmission(request.UserPrompt, mockAiResponse);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PromptSubmissionDto
        {
            Id = submission.Id,
            UserPrompt = request.UserPrompt,
            AiResponse = mockAiResponse,
            Status = "Success",
            IsViolation = false,
            CreatedAt = submission.CreatedAt
        };
    }
}
