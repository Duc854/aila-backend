using AILA.Application.Features.Questions.Dtos;

namespace AILA.Application.Common.Interfaces;

/// <summary>
/// Service xử lý Excel cho câu hỏi — parse import file và tạo template.
/// Được định nghĩa ở Application layer, implement ở Infrastructure layer.
/// </summary>
public interface IQuestionExcelService
{
    /// <summary>
    /// Tạo file Excel template mẫu để expert download về điền câu hỏi.
    /// </summary>
    /// <returns>Byte array của file .xlsx</returns>
    byte[] GenerateImportTemplate();

    /// <summary>
    /// Parse file Excel import, validate từng dòng và trả về kết quả chi tiết.
    /// Không thực hiện bất kỳ thao tác DB nào.
    /// </summary>
    /// <param name="fileStream">Stream của file .xlsx được upload</param>
    /// <returns>Danh sách các dòng đã parse kèm trạng thái hợp lệ</returns>
    List<QuestionImportRowDto> ParseImportFile(Stream fileStream);
}
