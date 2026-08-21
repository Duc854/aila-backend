using AILA.Application.Common.Interfaces.AI;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AILA.Infrastructure.Services.AI;

public class PrivacyService : IPrivacyService
{
    // Regex nhận diện các nhãn đã được mask/che sẵn để không bắt lỗi lại khi người dùng copy paste gợi ý
    private static readonly Regex MaskedPlaceholderRegex = new(
        @"\[(Email|Số điện thoại|CCCD|CCCD/CMND|Địa chỉ|Phone|Address|CMND)\]|\*{3,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Pre-compiled regex for performance
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    // Vietnamese phone numbers:
    // Hỗ trợ tất cả đầu số di động VN (03, 05, 07, 08, 09, +84, 84) với khoảng trắng, dấu chấm, gạch ngang
    // Ví dụ: 0912345678, 091 234 5678, 091.234.5678, 091-234-5678, +84 91 234 5678
    private static readonly Regex PhoneVnRegex = new(
        @"(?<!\d)(?:\+?84|0)[\s.\-]?[35789](?:[\s.\-]?\d){8}(?!\d)",
        RegexOptions.Compiled);

    // CCCD (12 chữ số) hoặc CMND (9 chữ số)
    private static readonly Regex CccdRegex = new(
        @"(?<!\d)(?:\d{12}|\d{9})(?!\d)",
        RegexOptions.Compiled);

    // Địa chỉ (nhận diện địa chỉ thực tế, tránh false positive với 'số 1', 'đường link', 'đường dẫn'...)
    private static readonly Regex AddressRegex = new(
        @"\b(?:(?i:Địa\s*chỉ)|(?i:số\s+\d+[a-z]?(?:[/]\d+)?\s+(?:đường|phố|ngõ|ngách|hẻm|phường|quận|ấp|thôn))|(?:đường|phố)\s+[A-ZÀ-Ỹ\p{Lu}][a-zà-ỹ\p{Ll}]+(?:\s+[A-ZÀ-Ỹ\p{Lu}][a-zà-ỹ\p{Ll}]+)+|(?i:quận\s+\d+|hẻm\s+\d+|ngõ\s+\d+|ngách\s+\d+))\b",
        RegexOptions.Compiled);

    public string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 1. Mask Email trước
        var masked = EmailRegex.Replace(input, "[Email]");

        // 2. Mask Số điện thoại (để không bị CCCD nhận nhầm)
        masked = PhoneVnRegex.Replace(masked, "[Số điện thoại]");

        // 3. Mask CCCD/CMND
        masked = CccdRegex.Replace(masked, "[CCCD]");

        // 4. Mask Địa chỉ (chỉ mask các đoạn chưa nằm trong placeholder mask)
        var unmaskedTokens = MaskedPlaceholderRegex.Matches(masked);
        masked = AddressRegex.Replace(masked, match =>
        {
            // Nếu đoạn match chính là một phần của placeholder đã mask như "[Địa chỉ]" thì giữ nguyên
            int idx = match.Index;
            foreach (Match m in unmaskedTokens)
            {
                if (idx >= m.Index && idx < m.Index + m.Length)
                {
                    return match.Value;
                }
            }
            return "[Địa chỉ]";
        });

        return masked;
    }

    public bool HasSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        // Loại bỏ các placeholder đã che sẵn (như [Email], [Số điện thoại], [CCCD], [Địa chỉ], ***) trước khi kiểm tra PII
        var unmasked = MaskedPlaceholderRegex.Replace(input, string.Empty);
        if (string.IsNullOrWhiteSpace(unmasked)) return false;

        if (EmailRegex.IsMatch(unmasked) || PhoneVnRegex.IsMatch(unmasked) || AddressRegex.IsMatch(unmasked))
            return true;

        // Bỏ qua số điện thoại đã nhận diện để không bị kiểm tra trùng sang CCCD
        var remaining = PhoneVnRegex.Replace(unmasked, string.Empty);
        return CccdRegex.IsMatch(remaining);
    }

    public List<string> GetSensitiveDataTypes(string input)
    {
        var types = new List<string>();

        if (string.IsNullOrEmpty(input)) return types;

        // Loại bỏ các placeholder đã che sẵn trước khi trích xuất loại PII
        var unmasked = MaskedPlaceholderRegex.Replace(input, string.Empty);
        if (string.IsNullOrWhiteSpace(unmasked)) return types;

        if (EmailRegex.IsMatch(unmasked)) types.Add("Email");
        if (PhoneVnRegex.IsMatch(unmasked)) types.Add("Số điện thoại");

        // Loại bỏ các đoạn đã khớp số điện thoại trước khi kiểm tra CCCD
        var remaining = PhoneVnRegex.Replace(unmasked, string.Empty);
        if (CccdRegex.IsMatch(remaining)) types.Add("CCCD/CMND");

        if (AddressRegex.IsMatch(unmasked)) types.Add("Địa chỉ");

        return types;
    }
}