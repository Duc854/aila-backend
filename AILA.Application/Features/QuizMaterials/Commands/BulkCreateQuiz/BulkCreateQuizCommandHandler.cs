using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.QuizMaterials.Commands.BulkCreateQuiz;

public sealed class BulkCreateQuizCommandHandler
    : IRequestHandler<
        BulkCreateQuizCommand,
        ResponseDto<object>>
{
    private readonly IUnitOfWork _uow;

    public BulkCreateQuizCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<object>> Handle(
    BulkCreateQuizCommand request,
    CancellationToken ct)
    {
        var material = await _uow.Materials
            .GetWithModuleAndCourseAsync(
                request.MaterialId,
                ct);

        if (material == null)
        {
            return ResponseDto<object>
                .FailResult(
                    "MATERIAL_NOT_FOUND",
                    "Không tìm thấy học liệu.");
        }

        if (material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<object>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền chỉnh sửa Quiz.");
        }

        if (material.MaterialType != Domain.Enums.MaterialType.Quiz)
        {
            return ResponseDto<object>
                .FailResult(
                    "INVALID_TYPE",
                    "Học liệu này không phải Quiz.");
        }

        await _uow.BeginTransactionAsync(ct);

        try
        {
            var quiz = await _uow.Materials
                .GetQuizDetailForExpertAsync(
                    request.MaterialId,
                    ct);

            if (quiz == null)
            {
                quiz = new QuizMaterial(
                    request.MaterialId,
                    request.TimeLimitMinutes,
                    request.PassingScore,
                    request.ShowCorrectAnswersAfterSubmission);

                await _uow.Repository<QuizMaterial>()
                    .AddAsync(quiz);
            }
            else
            {
                quiz.UpdateSetting(
                    request.TimeLimitMinutes,
                    request.PassingScore,
                    request.ShowCorrectAnswersAfterSubmission);
            }
            // Lấy toàn bộ câu hỏi hiện tại
            var existingQuestions = await _uow.Questions
                .GetByQuizIdAsync(
                    quiz.MaterialId,
                    ct);

            // Xóa toàn bộ
            foreach (var oldQuestion in existingQuestions)
            {
                _uow.Questions.Delete(oldQuestion);
            }
            await _uow.SaveChangesAsync(ct);
            var questionOrder = 1;

            foreach (var item in request.Questions)
            {
                var question = new Question(
                    quiz.MaterialId,
                    item.Content,
                    item.QuestionType,
                    questionOrder++);

                var answerOrder = 1;

                foreach (var answerItem in item.Answers)
                {
                    var answer = new AnswerOption(
                        question.Id,
                        answerItem.Content,
                        answerItem.IsCorrect,
                        answerOrder++);

                    question.AddAnswerOption(answer);
                }
                    question.ValidateAnswerOptions();
                await _uow.Questions.AddAsync(question);
            }
            await _uow.SaveChangesAsync(ct);

            await _uow.CommitTransactionAsync(ct);

            return ResponseDto<object>.SuccessResult(null!);
        }
        catch (InvalidOperationException ex)
        {
            await _uow.RollbackTransactionAsync(ct);

            return ResponseDto<object>.FailResult(
                "INVALID_QUESTION",
                ex.Message);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
