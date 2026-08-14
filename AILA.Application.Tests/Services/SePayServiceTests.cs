using AILA.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace AILA.Application.Tests.Services;

public class SePayServiceTests
{
    private readonly SePayService _sePayService;

    public SePayServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["SePay:WebhookSecret"] = "test-secret-key",
                ["SePay:ApiKey"] = "test-api-key",
                ["SePay:BankAccountNumber"] = "0369522588",
                ["SePay:BankAccountName"] = "NGUYEN VAN A",
                ["SePay:BankCode"] = "MB"
            })
            .Build();

        var logger = new Mock<ILogger<SePayService>>();
        _sePayService = new SePayService(configuration, logger.Object);
    }

    [Fact]
    public void VerifyWebhookSignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        const string testBody = """{"gateway":"MBBank","transactionDate":"2026-08-15 00:58:00","accountNumber":"0369522588","code":"AILA1786730303","transferAmount":50000}""";
        const string secretKey = "test-secret-key";
        const string timestamp = "1786730332";
        
        // Create expected signature using SePay format: timestamp + "." + body
        var payload = timestamp + "." + testBody;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();
        
        // Test both with and without sha256= prefix
        var signatureWithPrefix = $"sha256={expectedSignature}";

        // Act & Assert
        Assert.True(_sePayService.VerifyWebhookSignature(testBody, expectedSignature, timestamp));
        Assert.True(_sePayService.VerifyWebhookSignature(testBody, signatureWithPrefix, timestamp));
    }

    [Fact]
    public void VerifyWebhookSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        const string testBody = """{"test":"data"}""";
        const string timestamp = "1786730332";
        const string invalidSignature = "sha256=invalid-signature-hash";

        // Act & Assert
        Assert.False(_sePayService.VerifyWebhookSignature(testBody, invalidSignature, timestamp));
    }

    [Fact]
    public void VerifyWebhookSignature_WithEmptySignature_ShouldReturnFalse()
    {
        // Arrange
        const string testBody = """{"test":"data"}""";
        const string timestamp = "1786730332";

        // Act & Assert
        Assert.False(_sePayService.VerifyWebhookSignature(testBody, "", timestamp));
        Assert.False(_sePayService.VerifyWebhookSignature(testBody, "   ", timestamp));
    }
}