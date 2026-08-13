using AILA.Application.Common.Interfaces.AI;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AILA.Infrastructure.Services.AI;

public class PrivacyService : IPrivacyService
{
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

    // Địa chỉ (tránh false positive các từ thông dụng)
    private static readonly Regex AddressRegex = new(
        @"\b(?:Địa\s?chỉ|Số\s?\d+|phố\s+[A-Z\d\p{L}]|đường\s+[A-Z\d\p{L}]|huyện\s+[A-Z\d\p{L}]|tỉnh\s+[A-Z\d\p{L}]|thành\s?phố\s+[A-Z\d\p{L}]|quận\s+\d+|hẻm\s+\d+|ngõ\s+\d+)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 1. Mask Email trước
        var masked = EmailRegex.Replace(input, "[Email]");

        // 2. Mask Số điện thoại (để không bị CCCD nhận nhầm)
        masked = PhoneVnRegex.Replace(masked, "[Số điện thoại]");

        // 3. Mask CCCD/CMND
        masked = CccdRegex.Replace(masked, "[CCCD]");

        // 4. Mask Địa chỉ
        masked = AddressRegex.Replace(masked, "[Địa chỉ]");

        return masked;
    }

    public bool HasSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        if (EmailRegex.IsMatch(input) || PhoneVnRegex.IsMatch(input) || AddressRegex.IsMatch(input))
            return true;

        // Bỏ qua số điện thoại đã nhận diện để không bị kiểm tra trùng sang CCCD
        var remaining = PhoneVnRegex.Replace(input, string.Empty);
        return CccdRegex.IsMatch(remaining);
    }

    public List<string> GetSensitiveDataTypes(string input)
    {
        var types = new List<string>();

        if (string.IsNullOrEmpty(input)) return types;

        if (EmailRegex.IsMatch(input)) types.Add("Email");
        if (PhoneVnRegex.IsMatch(input)) types.Add("Số điện thoại");

        // Loại bỏ các đoạn đã khớp số điện thoại trước khi kiểm tra CCCD
        var remaining = PhoneVnRegex.Replace(input, string.Empty);
        if (CccdRegex.IsMatch(remaining)) types.Add("CCCD/CMND");

        if (AddressRegex.IsMatch(input)) types.Add("Địa chỉ");

        return types;
    }
}