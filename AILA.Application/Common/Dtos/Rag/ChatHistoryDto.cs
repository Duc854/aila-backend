using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.Rag;

public class ChatHistoryDto
{
    public List<CourseChatMessageDto> Messages { get; set; } = new();

    public void AddSystemMessage(string content)
    {
        Messages.Add(new CourseChatMessageDto
        {
            Role = "system",
            Content = content
        });
    }

    public void AddUserMessage(string content)
    {
        Messages.Add(new CourseChatMessageDto
        {
            Role = "user",
            Content = content
        });
    }

    public void AddAssistantMessage(string content)
    {
        Messages.Add(new CourseChatMessageDto
        {
            Role = "assistant",
            Content = content
        });
    }
}