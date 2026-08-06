using AILA.Domain.Common;
using System;
using System.Collections.Generic;

namespace AILA.Domain.Entities;

public class CourseChatSession : BaseEntity
{
    public Guid AccountId { get; private set; }
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;

    private readonly List<CourseChatMessage> _messages = new();
    public virtual IReadOnlyCollection<CourseChatMessage> Messages => _messages.AsReadOnly();

    private CourseChatSession() { }

    public CourseChatSession(Guid accountId, Guid courseId, string title)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        CourseId = courseId;
        Title = string.IsNullOrWhiteSpace(title) ? "Cuộc trò chuyện mới" : title.Trim();
    }

    public void UpdateTitle(string newTitle)
    {
        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            Title = newTitle.Trim();
            UpdateTimestamp();
        }
    }
}
