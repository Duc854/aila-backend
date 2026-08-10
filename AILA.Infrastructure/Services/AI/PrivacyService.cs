using AILA.Application.Common.Interfaces.AI;
using System.Text.RegularExpressions;

namespace AILA.Infrastructure.Services.AI;

public class PrivacyService : IPrivacyService
{
    // Pre-compiled regex for performance
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    // Vietnamese phone numbers — all format variants (10 digits total starting with 0/84/+84):
    // 0912345678, +84912345678, 84912345678, 084912345678
    // 091 234 5678, 091-234-5678, 091.234.5678
    // +84 91 234 5678, 84-91-234-5678
    private static readonly Regex PhoneVnRegex = new(
        @"(?<!\d)(?:\+?84|0)[\s.\-]?[3-9](?:[\s.\-]?\d){8}(?!\d)",
        RegexOptions.Compiled);

    // CCCD/CMND (Citizen Identity Card) - 9 or 12 digits
    private static readonly Regex CccdRegex = new(
        @"\b\d{9}\b|\b\d{12}\b",
        RegexOptions.Compiled);

    // Address patterns (refined to prevent false positives on common words like 'xã hội')
    private static readonly Regex AddressRegex = new(
        @"\b(?:Địa\s?chỉ|Số\s?\d+|phố\s+[A-Z\d\p{L}]|đường\s+[A-Z\d\p{L}]|huyện\s+[A-Z\d\p{L}]|tỉnh\s+[A-Z\d\p{L}]|thành\s?phố\s+[A-Z\d\p{L}]|quận\s+\d+|hẻm\s+\d+|ngõ\s+\d+)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Mask emails first
        var masked = EmailRegex.Replace(input, "[Email]");

        // Mask Vietnamese phone numbers
        masked = PhoneVnRegex.Replace(masked, "[Số điện thoại]");

        // Mask CCCD/CMND
        masked = CccdRegex.Replace(masked, "[CCCD]");

        // Mask addresses (keep some context but mask numbers)
        masked = AddressRegex.Replace(masked, "[Địa chỉ]");

        return masked;
    }

    public bool HasSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        return EmailRegex.IsMatch(input) ||
               PhoneVnRegex.IsMatch(input) ||
               CccdRegex.IsMatch(input) ||
               AddressRegex.IsMatch(input);
    }

    public List<string> GetSensitiveDataTypes(string input)
    {
        var types = new List<string>();

        if (string.IsNullOrEmpty(input)) return types;

        if (EmailRegex.IsMatch(input)) types.Add("Email");
        if (PhoneVnRegex.IsMatch(input)) types.Add("Số điện thoại");
        if (CccdRegex.IsMatch(input)) types.Add("CCCD/CMND");
        if (AddressRegex.IsMatch(input)) types.Add("Địa chỉ");

        return types;
    }
}