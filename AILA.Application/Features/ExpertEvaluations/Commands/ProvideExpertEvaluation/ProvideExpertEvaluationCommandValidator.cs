using FluentValidation;
using Microsoft.Extensions.Options;
using Shared.Models;

namespace AILA.Application.Features.ExpertEvaluations.Commands.ProvideExpertEvaluation
{
    /// <summary>
    /// AC-64.2: điểm ngoài khoảng hoặc thiếu phản hồi đều bị chặn trước khi vào handler,
    /// nên không có kết quả nào được tạo và trạng thái yêu cầu giữ nguyên.
    /// </summary>
    public sealed class ProvideExpertEvaluationCommandValidator
        : AbstractValidator<ProvideExpertEvaluationCommand>
    {
        public ProvideExpertEvaluationCommandValidator(IOptions<ExpertEvaluationSettings> options)
        {
            var settings = options.Value;

            RuleFor(x => x.RequestId)
                .NotEmpty()
                .WithMessage("Mã yêu cầu đánh giá không hợp lệ.");

            RuleFor(x => x.ExpertId)
                .NotEmpty()
                .WithMessage("Mã chuyên gia không hợp lệ.");

            RuleFor(x => x.OverallScore)
                .InclusiveBetween(settings.MinScore, settings.MaxScore)
                .WithMessage($"Điểm tổng phải nằm trong khoảng {settings.MinScore} đến {settings.MaxScore}.");

            RuleFor(x => x.OverallScore)
                .Must(score => decimal.Round(score, settings.ScoreDecimals) == score)
                .WithMessage($"Điểm tổng chỉ được có tối đa {settings.ScoreDecimals} chữ số thập phân.");

            RuleFor(x => x.Feedback)
                .NotEmpty()
                .WithMessage("Phản hồi của chuyên gia không được để trống.")
                .MaximumLength(settings.FeedbackMaxLength)
                .WithMessage($"Phản hồi không được vượt quá {settings.FeedbackMaxLength} ký tự.");

            // Khuyến nghị là tuỳ chọn: bỏ trống hợp lệ, chỉ chặn khi vượt độ dài cho phép.
            RuleFor(x => x.Recommendation!)
                .MaximumLength(settings.RecommendationMaxLength)
                .WithMessage($"Khuyến nghị không được vượt quá {settings.RecommendationMaxLength} ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Recommendation));
        }
    }
}
