using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Questions.Dtos;
using AILA.Domain.Enums;
using ClosedXML.Excel;

namespace AILA.Infrastructure.Services;

/// <summary>
/// Implementation của IQuestionExcelService dùng ClosedXML.
/// Cấu trúc file Excel:
/// <code>
/// | Content | QuestionType | Answer1 | IsCorrect1 | Answer2 | IsCorrect2 | Answer3 | IsCorrect3 | Answer4 | IsCorrect4 |
/// </code>
/// - QuestionType: SingleChoice | MultipleChoice
/// - IsCorrectN: TRUE | FALSE (không phân biệt hoa thường)
/// - Tối đa 4 đáp án mỗi câu hỏi
/// </summary>
public sealed class QuestionExcelService : IQuestionExcelService
{
    private const int MaxOptions = 4;

    // Tên các cột trong template — phải khớp giữa GenerateImportTemplate và ParseImportFile
    private static readonly string[] Headers =
    [
        "Content (*)",
        "QuestionType (*)",
        "Answer1 (*)",
        "IsCorrect1 (*)",
        "Answer2",
        "IsCorrect2",
        "Answer3",
        "IsCorrect3",
        "Answer4",
        "IsCorrect4"
    ];

    public byte[] GenerateImportTemplate()
    {
        using var workbook  = new XLWorkbook();
        var sheet           = workbook.Worksheets.Add("Questions");
        var headerRow       = sheet.Row(1);

        // ── Header row ────────────────────────────────────────────
        for (int col = 1; col <= Headers.Length; col++)
        {
            var cell = sheet.Cell(1, col);
            cell.Value = Headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // ── Ví dụ dòng 1: SingleChoice ────────────────────────────
        BuildExampleRow(
            sheet, rowNum: 2,
            content: "Thủ đô của Việt Nam là gì?",
            questionType: "SingleChoice",
            answers: [
                ("Hà Nội",   "TRUE"),
                ("TP.HCM",   "FALSE"),
                ("Đà Nẵng",  "FALSE"),
                ("Huế",      "FALSE")
            ]);

        // ── Ví dụ dòng 2: MultipleChoice ──────────────────────────
        BuildExampleRow(
            sheet, rowNum: 3,
            content: "Những ngôn ngữ nào thuộc họ C?",
            questionType: "MultipleChoice",
            answers: [
                ("C#",    "TRUE"),
                ("Java",  "FALSE"),
                ("C++",   "TRUE"),
                ("Python","FALSE")
            ]);

        // ── Ghi chú dưới bảng ─────────────────────────────────────
        sheet.Cell(5, 1).Value =
            "Ghi chú: (*) là cột bắt buộc. QuestionType chỉ nhận: SingleChoice hoặc MultipleChoice. " +
            "IsCorrect nhận: TRUE hoặc FALSE. SingleChoice phải có đúng 1 đáp án TRUE. " +
            "Xóa các dòng ví dụ trước khi điền dữ liệu thật.";
        sheet.Cell(5, 1).Style.Font.Italic = true;
        sheet.Cell(5, 1).Style.Font.FontColor = XLColor.FromHtml("#6B7280");
        sheet.Range(5, 1, 5, Headers.Length).Merge();

        // ── Auto-fit columns ──────────────────────────────────────
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildExampleRow(
        IXLWorksheet sheet,
        int rowNum,
        string content,
        string questionType,
        (string Answer, string IsCorrect)[] answers)
    {
        sheet.Cell(rowNum, 1).Value = content;
        sheet.Cell(rowNum, 2).Value = questionType;

        for (int i = 0; i < answers.Length && i < MaxOptions; i++)
        {
            sheet.Cell(rowNum, 3 + i * 2).Value     = answers[i].Answer;
            sheet.Cell(rowNum, 3 + i * 2 + 1).Value = answers[i].IsCorrect;
        }

        // Tô nền nhạt cho dòng ví dụ
        sheet.Row(rowNum).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
    }

    public List<QuestionImportRowDto> ParseImportFile(Stream fileStream)
    {
        var results = new List<QuestionImportRowDto>();

        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheets.First();

        // Bỏ qua dòng 1 (header), đọc từ dòng 2
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow < 2)
            return results;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row    = sheet.Row(rowNum);
            var errors = new List<string>();

            // Đọc Content
            var content = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(content))
                errors.Add("Cột Content không được để trống.");

            // Đọc QuestionType
            var questionTypeStr = row.Cell(2).GetString().Trim();
            QuestionType questionType;
            if (!Enum.TryParse(questionTypeStr, ignoreCase: true, out questionType))
                errors.Add($"QuestionType '{questionTypeStr}' không hợp lệ. Chỉ nhận: SingleChoice, MultipleChoice.");

            // Đọc đáp án (tối đa 4)
            var options = new List<AnswerOptionImportDto>();
            for (int i = 0; i < MaxOptions; i++)
            {
                var answerContent = row.Cell(3 + i * 2).GetString().Trim();
                var isCorrectStr  = row.Cell(3 + i * 2 + 1).GetString().Trim();

                // Cột Answer1 bắt buộc, Answer2-4 tùy chọn
                if (i == 0 && string.IsNullOrWhiteSpace(answerContent))
                {
                    errors.Add("Phải có ít nhất một đáp án (Answer1 không được để trống).");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(answerContent))
                    continue; // đáp án không có → bỏ qua

                bool isCorrect;
                if (!TryParseBoolean(isCorrectStr, out isCorrect))
                {
                    errors.Add($"IsCorrect{i + 1} '{isCorrectStr}' không hợp lệ. Chỉ nhận: TRUE hoặc FALSE.");
                    isCorrect = false;
                }

                options.Add(new AnswerOptionImportDto
                {
                    Content   = answerContent,
                    IsCorrect = isCorrect
                });
            }

            // Validate quy tắc domain sau khi có đủ options
            if (errors.Count == 0)
            {
                var correctCount = options.Count(o => o.IsCorrect);

                if (questionType == QuestionType.SingleChoice && correctCount != 1)
                    errors.Add("SingleChoice phải có đúng 1 đáp án đúng.");

                if (questionType == QuestionType.MultipleChoice && correctCount == 0)
                    errors.Add("MultipleChoice phải có ít nhất 1 đáp án đúng.");

                if (options.Count < 2)
                    errors.Add("Câu hỏi phải có ít nhất 2 đáp án.");
            }

            results.Add(new QuestionImportRowDto
            {
                RowNumber        = rowNum,
                Content          = content,
                QuestionType     = questionType,
                QuestionTypeName = questionType.ToString(),
                Options          = options,
                IsValid          = errors.Count == 0,
                Errors           = errors
            });
        }

        return results;
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        if (string.IsNullOrWhiteSpace(value)) { result = false; return false; }

        switch (value.ToUpperInvariant())
        {
            case "TRUE"  or "1" or "YES": result = true;  return true;
            case "FALSE" or "0" or "NO":  result = false; return true;
            default:                      result = false; return false;
        }
    }
}
