using AILA.Infrastructure.Services.AI;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

public class PrivacyService_AddressRegexFalsePositiveTests
{
    private readonly PrivacyService _sut = new();

    [Theory]
    [InlineData("Tôi muốn chọn phương án số 1")]
    [InlineData("Bước số 2 cần làm những việc gì?")]
    [InlineData("Lựa chọn số 10 là phù hợp nhất.")]
    [InlineData("Anh gửi cho em đường link tài liệu bài giảng")]
    [InlineData("Đường dẫn file cấu hình appsettings.json nằm ở đâu")]
    [InlineData("Học lập trình theo lộ trình đường dài")]
    public void HasSensitiveData_CommonPhrases_ReturnsFalse(string input)
    {
        var hasSensitive = _sut.HasSensitiveData(input);
        var sensitiveTypes = _sut.GetSensitiveDataTypes(input);

        Assert.False(hasSensitive);
        Assert.DoesNotContain("Địa chỉ", sensitiveTypes);
    }

    [Theory]
    [InlineData("Nhà tôi ở số 12 đường Nguyễn Huệ")]
    [InlineData("Địa chỉ: 456 phố Tràng Tiền")]
    [InlineData("Giao hàng tới hẻm 78 đường Lê Lợi, quận 1")]
    [InlineData("Số 25 ngõ 102 đường Giải Phóng")]
    public void HasSensitiveData_RealAddresses_ReturnsTrue(string input)
    {
        var hasSensitive = _sut.HasSensitiveData(input);
        var sensitiveTypes = _sut.GetSensitiveDataTypes(input);

        Assert.True(hasSensitive);
        Assert.Contains("Địa chỉ", sensitiveTypes);
    }

    [Fact]
    public void MaskSensitiveData_PreservesCommonWordsAndMasksRealAddresses()
    {
        var input = "Em chọn phương án số 1 và số 2, gửi tài liệu về địa chỉ số 12 đường Giải Phóng";
        var masked = _sut.MaskSensitiveData(input);

        Assert.Contains("phương án số 1", masked);
        Assert.Contains("số 2", masked);
        Assert.Contains("[Địa chỉ]", masked);
    }
}