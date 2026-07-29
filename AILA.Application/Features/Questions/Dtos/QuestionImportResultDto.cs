namespace AILA.Application.Features.Questions.Dtos;

/// <summary>
/// Kết quả trả về sau khi preview hoặc import questions từ file Excel.
/// </summary>
public sealed class QuestionImportResultDto
{
    /// <summary>Tổng số dòng trong file (không tính header).</summary>
    public int TotalRows { get; init; }

    /// <summary>Số dòng hợp lệ.</summary>
    public int ValidRows { get; init; }

    /// <summary>Số dòng có lỗi.</summary>
    public int InvalidRows { get; init; }

    /// <summary>
    /// Chi tiết từng dòng — cả hợp lệ lẫn lỗi.
    /// Frontend dùng để hiển thị bảng review trước khi confirm.
    /// </summary>
    public List<QuestionImportRowDto> Rows { get; init; } = new();

    /// <summary>
    /// Chỉ có giá trị khi import thật (dryRun = false) và thành công.
    /// Danh sách câu hỏi vừa được tạo.
    /// </summary>
    public List<QuestionDto>? ImportedQuestions { get; init; }
}
