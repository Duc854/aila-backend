using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Enums
{
    public enum QuizAttemptStatus
    {
        InProgress,
        Submitted,

        /// <summary>
        /// Lượt làm bài đã quá hạn nộp theo đồng hồ phía server và bị đóng lại mà không có
        /// bài nộp hợp lệ. Trạng thái kết thúc: không thể tiếp tục, không thể nộp, và
        /// không xuất hiện trong lịch sử kết quả (vốn chỉ lấy các lượt Submitted).
        /// </summary>
        Expired
    }
}
