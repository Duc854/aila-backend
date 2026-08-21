using AILA.Application.Common.Dtos.Rag;
using AILA.Application.Common.Interfaces.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Services.AI;

public class PromptBuilder : IPromptBuilder
{
    public Task<ChatHistoryDto> BuildAsync(
        string question,
        VectorSearchResult searchResult,
        List<CourseChatMessageDto> history,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistoryDto();

        // Lấy từ RagChatService cũ
        string systemInstruction;
        if (searchResult.HasRelevantContent && !string.IsNullOrWhiteSpace(searchResult.ContextText))
        {
            systemInstruction = $@"Bạn là trợ lý AI thông minh phụ trách giải đáp thắc mắc cho Học viên trong khóa học.

Dưới đây là NỘI DUNG TÀI LIỆU BÀI HỌC liên quan trực tiếp đến câu hỏi được trích xuất từ hệ thống:

{searchResult.ContextText}

YÊU CẦU TRẢ LỜI:
1. Hãy sử dụng NỘI DUNG TÀI LIỆU BÀI HỌC ở trên để giải đáp chính xác, rõ ràng và mạch lạc cho Học viên.
2. Trả lời bằng tiếng Việt, thái độ hỗ trợ nhiệt tình, dễ hiểu.";
        }
        else
        {
            // Lấy từ RagChatService cũ
            systemInstruction = @"Bạn là trợ lý AI thông minh phụ trách hỗ trợ và giải đáp thắc mắc cho Học viên trong khóa học.

HƯỚNG DẪN TRẢ LỜI:
1. NẾU NGƯỜI DÙNG CHÀO HỎI HOẶC GIAO TIẾP XÃ GIAO (ví dụ: 'hello', 'hi', 'chào bạn', 'cảm ơn'): Hãy chào lại một cách thân thiện, tự nhiên và sẵn sàng giải đáp các câu hỏi về khóa học. TUYỆT ĐỐI KHÔNG tự ý đưa ra các bài học cụ thể hay giới thiệu tài liệu khi người dùng chưa hỏi.
2. NẾU NGƯỜI DÙNG HỎI KIẾN THỨC CHUNG HOẶC NGOÀI KHÓA HỌC: Hãy vận dụng kiến thức chuyên môn rộng lớn của bạn để giải đáp chi tiết, chu đáo và hữu ích cho Học viên (TUYỆT ĐỐI KHÔNG từ chối trả lời hoặc bảo 'tôi không biết').
3. Trả lời bằng tiếng Việt, lịch sự, thân thiện và mạch lạc.";
        }

        chatHistory.AddSystemMessage(systemInstruction);

        // Lấy từ RagChatService cũ
        foreach (var msg in history)
        {
            if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddAssistantMessage(msg.Content);
            }
            else
            {
                chatHistory.AddUserMessage(msg.Content);
            }
        }

        chatHistory.AddUserMessage(question);

        return Task.FromResult(chatHistory);
    }
}