namespace AILA.Application.Common.Interfaces.AI;
public interface IPrivacyService {
    string MaskSensitiveData(string input);
    bool HasSensitiveData(string text); // Kiểm tra có PII không
    List<string> GetSensitiveDataTypes(string text); // Lấy danh sách loại PII
}
