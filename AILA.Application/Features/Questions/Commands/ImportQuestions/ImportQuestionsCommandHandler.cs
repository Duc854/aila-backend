using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Questions.Dtos;
using AILA.Application.Features.Questions.Mapping;
using AILA.Domain.Entities;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Questions.Commands.ImportQuestions;

public sealed class ImportQuestionsCommandHandler
    : IRequestHandler<ImportQuestionsCommand, ResponseDto<QuestionImportResultDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IQuestionExcelService _excelService;

    public ImportQuestionsCommandHandler(
        IUnitOfWork uow,
        IQuestionExcelService excelService)
    {
        _uow = uow;
        _excelService = excelService;
    }

    public async Task<ResponseDto<QuestionImportResultDto>> Handle(
        ImportQuestionsCommand request,
        CancellationToken ct)
    {
        // 1. Verify quiz tồn tại và thuộc expert
        var quiz = await _uow.Materials
            .GetQuizDetailForExpertAsync(request.QuizMaterialId, ct);

        if (quiz == null)
            return ResponseDto<QuestionImportResultDto>
                .FailResult("QUIZ_NOT_FOUND", "Không tìm thấy Quiz.");

        if (quiz.Material.Module.Course.ExpertId != request.ExpertId)
            return ResponseDto<QuestionImportResultDto>
                .FailResult("FORBIDDEN", "Bạn không có quyền thêm câu hỏi vào Quiz này.");

        // 2. Parse file Excel
        List<QuestionImportRowDto> rows;
        try
        {
            rows = _excelService.ParseImportFile(request.FileStream);
        }
        catch (Exception)
        {
            return ResponseDto<QuestionImportResultDto>
                .FailResult("INVALID_FILE", "File không hợp lệ hoặc không đúng định dạng template.");
        }

        var validRows   = rows.Where(r => r.IsValid).ToList();
        var invalidRows = rows.Where(r => !r.IsValid).ToList();

        // 3. DryRun: trả về preview, không lưu
        if (request.DryRun)
        {
            return ResponseDto<QuestionImportResultDto>.SuccessResult(new QuestionImportResultDto
            {
                TotalRows   = rows.Count,
                ValidRows   = validRows.Count,
                InvalidRows = invalidRows.Count,
                Rows        = rows
            });
        }

        // 4. Import thật: chỉ lưu các dòng hợp lệ
        if (validRows.Count == 0)
            return ResponseDto<QuestionImportResultDto>
                .FailResult("NO_VALID_ROWS", "Không có dòng hợp lệ nào để import.");

        // Lấy OrderIndex hiện tại để tiếp nối
        var existingQuestions = await _uow.Questions
            .GetByQuizIdAsync(request.QuizMaterialId, ct);

        var nextOrder = existingQuestions.Any()
            ? existingQuestions.Max(q => q.OrderIndex) + 1
            : 1;

        var createdQuestions = new List<Question>();

        foreach (var row in validRows)
        {
            var question = new Question(
                request.QuizMaterialId,
                row.Content,
                row.QuestionType,
                nextOrder++);

            for (int i = 0; i < row.Options.Count; i++)
            {
                var opt = row.Options[i];
                var answerOption = new AnswerOption(
                    question.Id,
                    opt.Content,
                    opt.IsCorrect,
                    i + 1);

                question.AddAnswerOption(answerOption);
            }

            await _uow.Questions.AddAsync(question);
            createdQuestions.Add(question);
        }

        await _uow.SaveChangesAsync(ct);

        var importedDtos = createdQuestions
            .Select(QuestionMapper.MapToDto)
            .ToList();

        return ResponseDto<QuestionImportResultDto>.SuccessResult(new QuestionImportResultDto
        {
            TotalRows          = rows.Count,
            ValidRows          = validRows.Count,
            InvalidRows        = invalidRows.Count,
            Rows               = rows,
            ImportedQuestions  = importedDtos
        });
    }
}
