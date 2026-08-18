using AILA.Infrastructure.Services.AI;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using Xunit;

namespace AILA.Application.Tests.UnitTests;

public class RoleParserService_ParseRolesTests
{
    private readonly RoleParserService _sut = new(NullLogger<RoleParserService>.Instance);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParseRolesAsync_EmptyInput_ReturnsUnidentified(string? input)
    {
        var result = await _sut.ParseRolesAsync(input!);

        Assert.False(result.IsSuccess);
        Assert.Equal("Chưa xác định", result.UserRole);
        Assert.Equal("Chưa xác định", result.AIRole);
    }

    [Fact]
    public async Task ParseRolesAsync_StandardVietnamesePattern_ExtractsCorrectRoles()
    {
        var aiTask = "Bạn LÀ Senior Dev (Mentor). Người đang chat với bạn LÀ Junior Dev.";
        var result = await _sut.ParseRolesAsync(aiTask);

        Assert.True(result.IsSuccess);
        Assert.Equal("Mentor", result.AIRole);
        Assert.Equal("Junior Dev", result.UserRole);
    }

    [Fact]
    public async Task ParseRolesAsync_NaturalVietnamesePattern_DongVaiLa_ExtractsCorrectRoles()
    {
        var aiTask = "Đóng vai là một khách hàng khó tính. Học viên đóng vai nhân viên chăm sóc khách hàng.";
        var result = await _sut.ParseRolesAsync(aiTask);

        Assert.True(result.IsSuccess);
        Assert.Equal("một khách hàng khó tính", result.AIRole);
        Assert.Equal("nhân viên chăm sóc khách hàng", result.UserRole);
    }

    [Fact]
    public async Task ParseRolesAsync_EnglishFormat_YouAre_ExtractsCorrectRoles()
    {
        var aiTask = "You are an experienced HR Manager. The user is a job applicant.";
        var result = await _sut.ParseRolesAsync(aiTask);

        Assert.True(result.IsSuccess);
        Assert.Equal("experienced HR Manager", result.AIRole);
        Assert.Equal("job applicant", result.UserRole);
    }

    [Fact]
    public async Task ParseRolesAsync_RoleColonFormat_ExtractsCorrectRoles()
    {
        var aiTask = "AI role: Tech Lead, User role: Intern Backend";
        var result = await _sut.ParseRolesAsync(aiTask);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tech Lead", result.AIRole);
        Assert.Equal("Intern Backend", result.UserRole);
    }

    [Fact]
    public async Task ParseRolesAsync_CleansPunctuationsAndQuotes()
    {
        var aiTask = "Bạn là 'Chuyên viên tư vấn tài chính'. Người dùng đóng vai \"Khách hàng cá nhân\".";
        var result = await _sut.ParseRolesAsync(aiTask);

        Assert.True(result.IsSuccess);
        Assert.Equal("Chuyên viên tư vấn tài chính", result.AIRole);
        Assert.Equal("Khách hàng cá nhân", result.UserRole);
    }
}