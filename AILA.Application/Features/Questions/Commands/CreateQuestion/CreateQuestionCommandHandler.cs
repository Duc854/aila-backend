using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Mapping;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.CreateQuestion;

public sealed class CreateQuestionCommandHandler
    : IRequestHandler<
        CreateQuestionCommand,
        ResponseDto<QuestionDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateQuestionCommandHandler(
        IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ResponseDto<QuestionDto>> Handle(
        CreateQuestionCommand request,
        CancellationToken ct)
    {
        // 1. Kiểm tra Quiz
        var quiz = await _uow.Materials
            .GetQuizDetailForExpertAsync(
                request.QuizMaterialId,
                ct);

        if (quiz == null)
        {
            return ResponseDto<QuestionDto>
                .FailResult(
                    "QUIZ_NOT_FOUND",
                    "Không tìm thấy Quiz.");
        }

        // 2. Kiểm tra quyền Expert
        if (quiz.Material.Module.Course.ExpertId != request.ExpertId)
        {
            return ResponseDto<QuestionDto>
                .FailResult(
                    "FORBIDDEN",
                    "Bạn không có quyền thêm câu hỏi.");
        }

        // 3. Lấy danh sách Question hiện tại từ DB
        var questions = await _uow.Questions
            .GetByQuizIdAsync(
                request.QuizMaterialId,
                ct);
        Console.WriteLine($"QuizId = {request.QuizMaterialId}");
        Console.WriteLine($"Questions = {questions.Count}");

        foreach (var q in questions)
        {
            Console.WriteLine($"Order = {q.OrderIndex}");
        }

        // 4. Tính OrderIndex
        var nextOrder = questions.Any()
            ? questions.Max(x => x.OrderIndex) + 1
            : 1;

        // 5. Tạo Question
        var question = new Question(
            request.QuizMaterialId,
            request.Content,
            request.QuestionType,
            nextOrder);

        // 6. Lưu
        await _uow.Questions.AddAsync(question);

        await _uow.SaveChangesAsync(ct);

        return ResponseDto<QuestionDto>
            .SuccessResult(
                QuestionMapper.MapToDto(question));
    }
}