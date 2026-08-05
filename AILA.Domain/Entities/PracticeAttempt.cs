namespace AILA.Domain.Entities;
using AILA.Domain.Common;
using AILA.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class PracticeAttempt : BaseEntity
{
    public Guid EnrollmentId { get; private set; }
    public Guid MaterialId { get; private set; }
    public PracticeAttemptStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public decimal? FinalScore { get; private set; }

    /// <summary>Gợi ý cải thiện tổng hợp từ AI sau khi HOÀN THÀNH bài thực hành</summary>
    public string OverallSuggestion { get; private set; } = string.Empty;

    // Navigation properties
    [ForeignKey("AttemptId")]
    public List<PromptSubmission> Submissions { get; private set; } = new();

    private PracticeAttempt() { }

    public PracticeAttempt(Guid enrollmentId, Guid materialId)
    {
        Id = Guid.NewGuid();
        EnrollmentId = enrollmentId;
        MaterialId = materialId;
        Status = PracticeAttemptStatus.InProgress;
        Submissions = new List<PromptSubmission>();
    }

    /// <summary>Kiểm tra có thể submit thêm không (dựa trên MaxPromptAttempts từ Material)</summary>
    public bool CanSubmitMore(int maxAttempts) =>
        Status == PracticeAttemptStatus.InProgress && Submissions.Count(s => !s.IsRejected) < maxAttempts;

    /// <summary>Thêm một lượt submit prompt thành công (DDD Aggregate Method)</summary>
    public PromptSubmission AddSubmission(string userPrompt, string aiResponse)
    {
        var submission = new PromptSubmission(Id, userPrompt, aiResponse);
        Submissions.Add(submission);
        return submission;
    }

    /// <summary>Thêm một lượt submit prompt vi phạm/bị từ chối (DDD Aggregate Method)</summary>
    public PromptSubmission AddRejectedSubmission(string userPrompt, string reason, string policyName)
    {
        var submission = new PromptSubmission(Id, userPrompt, string.Empty);
        submission.Reject(reason, policyName);
        Submissions.Add(submission);
        return submission;
    }

    public void Complete(decimal score, string overallSuggestion = "")
    {
        Status = PracticeAttemptStatus.Completed;
        FinalScore = score;
        OverallSuggestion = overallSuggestion ?? string.Empty;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Abandon()
    {
        Status = PracticeAttemptStatus.Abandoned;
        UpdateTimestamp();
    }
}