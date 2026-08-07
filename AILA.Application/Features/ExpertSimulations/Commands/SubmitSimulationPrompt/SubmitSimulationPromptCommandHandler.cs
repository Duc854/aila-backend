using AILA.Application.Common.Dtos.AI;
using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.AI;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using MediatR;
using System;
using System.Linq;
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
    private readonly IPracticeChatService _chatService;

    public SubmitSimulationPromptCommandHandler(
        IUnitOfWork unitOfWork,
        IAIPracticeMaterialRepository materialRepo,
        IPrivacyService privacyService,
        IModerationService moderationService,
        IQuotaService quotaService,
        IPracticeChatService chatService)
    {
        _unitOfWork = unitOfWork;
        _materialRepo = materialRepo;
        _privacyService = privacyService;
        _moderationService = moderationService;
        _quotaService = quotaService;
        _chatService = chatService;
    }

    public async Task<PromptSubmissionDto> Handle(SubmitSimulationPromptCommand request, CancellationToken cancellationToken)
    {
        // 1. Get ExpertSimulationAttempt (load kèm submissions để build history)
        var attempt = await _unitOfWork.Repository<ExpertSimulationAttempt>().GetByIdAsync(request.SimulationAttemptId)
            ?? throw new NotFoundException(nameof(ExpertSimulationAttempt), request.SimulationAttemptId);

        var material = await _materialRepo.GetByIdAsync(attempt.MaterialId)
            ?? throw new NotFoundException("AIPracticeMaterial", attempt.MaterialId);

        // 2. Check maximum prompt attempts — AF-07 (BR-04)
        if (!attempt.CanSubmitMore(material.MaxPromptAttempts))
        {
            throw new InvalidOperationException($"Simulation đã đạt giới hạn tối đa ({material.MaxPromptAttempts} lượt) hoặc đã kết thúc.");
        }

        // 3. PII masking + basic validation (BR-07, AF-04)
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

        var sanitizedPrompt = _privacyService.MaskSensitiveData(request.UserPrompt);

        if (_privacyService.HasSensitiveData(request.UserPrompt))
        {
            var piiTypes = _privacyService.GetSensitiveDataTypes(request.UserPrompt);
            var validationReason = $"Phát hiện thông tin cá nhân: {string.Join(", ", piiTypes)}.";

            var rejectedSubmission = attempt.AddRejectedSubmission(
                sanitizedPrompt,
                validationReason,
                "PIIProtection");

            await _unitOfWork.Repository<UserViolationRecord>().AddAsync(new UserViolationRecord(
                attempt.ExpertId,
                "PromptValidationViolation",
                "PIIProtection",
                validationReason,
                attemptId: attempt.Id,
                severity: "Medium"));

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

        // 4. Content Moderation (BR-07)
        var (isSafe, moderationReason) = await _moderationService.CheckContentSafetyAsync(sanitizedPrompt, cancellationToken);
        if (!isSafe)
        {
            var rejectedSubmission = attempt.AddRejectedSubmission(
                sanitizedPrompt,
                moderationReason ?? "Vi phạm quy chuẩn an toàn nội dung",
                "ContentModeration");

            await _unitOfWork.Repository<UserViolationRecord>().AddAsync(new UserViolationRecord(
                attempt.ExpertId,
                "ContentModerationViolation",
                "ContentModeration",
                moderationReason ?? "Vi phạm quy chuẩn an toàn nội dung",
                attemptId: attempt.Id,
                severity: "High"));

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

        // 5. Quota check (BR-01, AF-03)
        var quotaResult = await _quotaService.CheckQuotaAsync(attempt.ExpertId, 1000, 0.80f, cancellationToken);
        if (!quotaResult.IsAllowed)
        {
            return new PromptSubmissionDto
            {
                Id = Guid.NewGuid(),
                UserPrompt = sanitizedPrompt,
                AiResponse = string.Empty,
                Status = "QuotaExceeded",
                IsViolation = false,
                WarningMessage = quotaResult.WarningMessage ?? "Hạn mức Token AI của Expert không đủ để tiếp tục simulation.",
                CreatedAt = DateTime.UtcNow
            };
        }

        // 6. Build conversation history từ submissions đã lưu (BR-02 — dùng draft config)
        var existingSubmissions = (await _unitOfWork.Repository<PromptSubmission>()
            .FindAsync(s => s.AttemptId == attempt.Id))
            .Where(s => !s.IsRejected)
            .OrderBy(s => s.CreatedAt)
            .ToList();

        var history = existingSubmissions
            .SelectMany(s => new[]
            {
                new ChatMessage("user",      s.UserPrompt),
                new ChatMessage("assistant", s.AiResponse),
            })
            .ToList();

        // 7. Gọi AI thật (Step 7-9, AF-05/06) — dùng AITask làm system prompt
        string aiResponse;
        try
        {
            aiResponse = await _chatService.GetChatResponseAsync(
                systemPrompt: material.AITask,
                userPrompt: sanitizedPrompt,
                conversationHistory: history,
                attemptId: attempt.Id,
                accountId: attempt.ExpertId,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // AF-06 — AI response generation fails: không lưu, báo lỗi để expert retry
            return new PromptSubmissionDto
            {
                Id = Guid.NewGuid(),
                UserPrompt = sanitizedPrompt,
                AiResponse = string.Empty,
                Status = "AiError",
                IsViolation = false,
                WarningMessage = $"AI tạm thời không phản hồi. Vui lòng thử lại. ({ex.Message})",
                CreatedAt = DateTime.UtcNow
            };
        }

        // 8. Mask AI response và lưu submission
        var sanitizedAiResponse = _privacyService.MaskSensitiveData(aiResponse);
        var submission = attempt.AddSubmission(sanitizedPrompt, sanitizedAiResponse);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PromptSubmissionDto
        {
            Id = submission.Id,
            UserPrompt = sanitizedPrompt,
            AiResponse = sanitizedAiResponse,
            Status = "Success",
            IsViolation = false,
            WarningMessage = quotaResult.WarningMessage,
            CreatedAt = submission.CreatedAt
        };
    }
}
