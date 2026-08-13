using System;
using System.Collections.Generic;

namespace AILA.Application.Features.AIPricing.Dtos;

public class AIPricingListResponseDto
{
    /// <summary>
    /// Cờ báo cho UI: true nếu đã có cấu hình giá trong hệ thống, false nếu chưa có gì để UI hiện thông báo/modal yêu cầu nhập giá
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Model AI mặc định đang dùng (vd: llama-3.3-70b-versatile)
    /// </summary>
    public string DefaultModelId { get; set; } = "llama-3.3-70b-versatile";

    /// <summary>
    /// Tỷ giá quy đổi USD -> VND tham khảo (mặc định 25.400 VND / 1 USD)
    /// </summary>
    public decimal ExchangeRateUsdToVnd { get; set; } = 25400m;

    /// <summary>
    /// Danh sách chi tiết các model và đơn giá
    /// </summary>
    public List<AIPricingConfigDto> Items { get; set; } = new();
}

public class AIPricingConfigDto
{
    public Guid Id { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal CostPerInputToken { get; set; }
    public decimal CostPerOutputToken { get; set; }

    /// <summary>
    /// Đơn giá hiển thị cho 1 triệu Input Tokens ($/1M Tokens)
    /// </summary>
    public decimal CostPer1MInputTokens => Math.Round(CostPerInputToken * 1_000_000m, 4);

    /// <summary>
    /// Đơn giá hiển thị cho 1 triệu Output Tokens ($/1M Tokens)
    /// </summary>
    public decimal CostPer1MOutputTokens => Math.Round(CostPerOutputToken * 1_000_000m, 4);

    /// <summary>
    /// Đơn giá hiển thị cho 1 triệu Input Tokens quy đổi VNĐ (VNĐ/1M Tokens)
    /// </summary>
    public decimal CostPer1MInputTokensVnd => Math.Round(CostPerInputToken * 1_000_000m * 25400m, 0);

    /// <summary>
    /// Đơn giá hiển thị cho 1 triệu Output Tokens quy đổi VNĐ (VNĐ/1M Tokens)
    /// </summary>
    public decimal CostPer1MOutputTokensVnd => Math.Round(CostPerOutputToken * 1_000_000m * 25400m, 0);

    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAIPricingRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = "Groq";
    public decimal CostPerInputToken { get; set; }
    public decimal CostPerOutputToken { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
}

public class UpdateAIPricingRequest
{
    public string? ModelId { get; set; }
    public string ServiceName { get; set; } = "Groq";
    public decimal CostPerInputToken { get; set; }
    public decimal CostPerOutputToken { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
}
