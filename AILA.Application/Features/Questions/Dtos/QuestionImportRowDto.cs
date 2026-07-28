using AILA.Domain.Enums;

namespace AILA.Application.Features.Questions.Dtos;

/// <summary>
/// Đại diện cho một dòng dữ liệu được parse từ file Excel import.
/// Dùng cho cả preview (dryRun) và import thật.
/// </summary>
public sealed class QuestionImportRowDto
{
    /// <summary>Số thứ tự dòng trong file Excel (bắt đầu từ 2, vì dòng 1 là header).</summary>
    public int RowNumber { get; init; }

    public string Content { get; init; } = string.Empty;

    public QuestionType QuestionType { get; init; }

    public string QuestionTypeName { get; init; } = string.Empty;

    /// <summary>Các đáp án được parse từ cột AnswerOption1..N.</summary>
    public List<AnswerOptionImportDto> Options { get; init; } = new();

    /// <summary>True nếu dòng này hợp lệ và có thể import.</summary>
    public bool IsValid { get; init; }

    /// <summary>Danh sách lỗi validate trên dòng này.</summary>
    public List<string> Errors { get; init; } = new();
}

public sealed class AnswerOptionImportDto
{
    public string Content { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}
