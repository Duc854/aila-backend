using AILA.Infrastructure.Services.AI;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT33_GetSensitiveDataTypes — <see cref="PrivacyService.GetSensitiveDataTypes"/>
/// Module: AIPractice · CC = 6 · 9 test case
///
/// Nhánh: B1 = IsNullOrEmpty (trả list rỗng) · B2 = EmailRegex · B3 = PhoneVnRegex
///        B4 = CccdRegex · B5 = AddressRegex
///
/// Bốn nhánh B2–B5 ĐỘC LẬP (không phải if/else) nên một chuỗi có thể sinh nhiều loại PII;
/// thứ tự phần tử trong danh sách là cố định theo thứ tự kiểm tra trong code.
/// Đây là hàng rào chặn dữ liệu cá nhân lọt vào prompt gửi cho LLM.
/// </summary>
public class UT33_PrivacyService_GetSensitiveDataTypesTests
{
    private readonly PrivacyService _sut = new();

    /// <summary>UTCID01 · B1=T · Type A — input null.</summary>
    [Fact]
    public void UTCID01_NullInput_ReturnsEmptyList()
    {
        Assert.Empty(_sut.GetSensitiveDataTypes(null!));
    }

    /// <summary>UTCID02 · B1=T · Type B — chuỗi rỗng (biên dưới).</summary>
    [Fact]
    public void UTCID02_EmptyInput_ReturnsEmptyList()
    {
        Assert.Empty(_sut.GetSensitiveDataTypes(string.Empty));
    }

    /// <summary>UTCID03 · B2..B5 đều = F · Type N — văn bản sạch, không chứa PII.</summary>
    [Fact]
    public void UTCID03_CleanText_ReturnsEmptyList()
    {
        Assert.Empty(_sut.GetSensitiveDataTypes("Hay viet mot email cam on khach hang"));
    }

    /// <summary>UTCID04 · B2=T · Type N — chỉ chứa email.</summary>
    [Fact]
    public void UTCID04_EmailOnly_ReturnsEmail()
    {
        var result = _sut.GetSensitiveDataTypes("Lien he toi qua abc@gmail.com nhe");

        Assert.Equal(new[] { "Email" }, result);
    }

    /// <summary>UTCID05 · B3=T · Type N — chỉ chứa số điện thoại Việt Nam 10 số.</summary>
    [Fact]
    public void UTCID05_PhoneOnly_ReturnsPhone()
    {
        var result = _sut.GetSensitiveDataTypes("So dien thoai cua toi la 0912345678");

        Assert.Equal(new[] { "Số điện thoại" }, result);
    }

    /// <summary>UTCID06 · B4=T · Type N — chỉ chứa CCCD 12 số.</summary>
    [Fact]
    public void UTCID06_CccdOnly_ReturnsCccd()
    {
        var result = _sut.GetSensitiveDataTypes("Ma dinh danh 123456789012 cua toi");

        Assert.Equal(new[] { "CCCD/CMND" }, result);
    }

    /// <summary>UTCID07 · B5=T · Type N — chỉ chứa từ khóa địa chỉ.</summary>
    [Fact]
    public void UTCID07_AddressOnly_ReturnsAddress()
    {
        var result = _sut.GetSensitiveDataTypes("Địa chỉ nha toi o gan day");

        Assert.Equal(new[] { "Địa chỉ" }, result);
    }

    /// <summary>UTCID08 · B2=T và B3=T · Type B — hai loại PII cùng lúc, đúng thứ tự kiểm tra.</summary>
    [Fact]
    public void UTCID08_EmailAndPhone_ReturnsBothInCheckOrder()
    {
        var result = _sut.GetSensitiveDataTypes("Mail abc@gmail.com hoac goi 0912345678");

        Assert.Equal(new[] { "Email", "Số điện thoại" }, result);
    }

    /// <summary>UTCID09 · B2..B5 đều = T · Type B — đủ cả 4 loại PII (biên trên).</summary>
    [Fact]
    public void UTCID09_AllFourPiiTypes_ReturnsAllInCheckOrder()
    {
        var result = _sut.GetSensitiveDataTypes(
            "Lien he abc@gmail.com, goi 0912345678, CCCD 123456789, Địa chỉ nha rieng");

        Assert.Equal(new[] { "Email", "Số điện thoại", "CCCD/CMND", "Địa chỉ" }, result);
    }
}
