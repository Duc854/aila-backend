namespace AILA.Application.Features.Questions.Queries.GetImportTemplate;

public sealed class GetImportTemplateResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>Byte array của file .xlsx khi thành công.</summary>
    public byte[]? FileContent { get; init; }

    public static GetImportTemplateResult Ok(byte[] fileContent) => new()
    {
        Success = true,
        FileContent = fileContent
    };

    public static GetImportTemplateResult Fail(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
