using AILA.Application.Features.Quizzes;
using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT08_IsAnswerCorrect — <see cref="QuizGrading.IsAnswerCorrect"/>
/// Module: Quiz · CC = 5 · 9 test case
///
/// Nhánh: B1 = correctIds.Count == 0 · B2 = toán tử ?? khi selectedOptionIds null
///        B3 = selected.Count &gt; 0 · B4 = selected.SetEquals(correctIds)
///
/// Chấm kiểu tất-cả-hoặc-không: chọn THIẾU hoặc chọn THỪA đều sai (cùng B4 = F
/// nhưng là hai lớp tương đương khác nhau ⇒ tách 2 test case).
/// </summary>
public class UT08_QuizGrading_IsAnswerCorrectTests
{
    private static readonly Guid QuizMaterialId = Guid.NewGuid();

    /// <summary>Tạo câu hỏi với danh sách cờ IsCorrect cho từng đáp án theo thứ tự.</summary>
    private static Question BuildQuestion(params bool[] correctFlags)
    {
        var question = new Question(QuizMaterialId, "Prompt engineering là gì?", QuestionType.MultipleChoice, 1);

        for (var i = 0; i < correctFlags.Length; i++)
        {
            question.AddAnswerOption(
                new AnswerOption(question.Id, $"Đáp án {(char)('A' + i)}", correctFlags[i], i + 1));
        }

        return question;
    }

    private static Guid OptionAt(Question question, int index) =>
        question.AnswerOptions.ElementAt(index).Id;

    /// <summary>UTCID01 · B1=F, B3=T, B4=T · Type N — 1 đáp án đúng, chọn đúng.</summary>
    [Fact]
    public void UTCID01_SingleCorrectOptionSelected_ReturnsTrue()
    {
        var question = BuildQuestion(true, false, false);

        Assert.True(QuizGrading.IsAnswerCorrect(question, new[] { OptionAt(question, 0) }));
    }

    /// <summary>UTCID02 · B1=T · Type A — câu hỏi không có đáp án đúng nào ⇒ không chấm được.</summary>
    [Fact]
    public void UTCID02_QuestionWithoutCorrectOption_ReturnsFalse()
    {
        var question = BuildQuestion(false, false);

        Assert.False(QuizGrading.IsAnswerCorrect(question, new[] { OptionAt(question, 0) }));
    }

    /// <summary>UTCID03 · B1=T · Type B — câu hỏi không có đáp án nào (tập rỗng).</summary>
    [Fact]
    public void UTCID03_QuestionWithoutAnyOption_ReturnsFalse()
    {
        var question = BuildQuestion();

        Assert.False(QuizGrading.IsAnswerCorrect(question, Array.Empty<Guid>()));
    }

    /// <summary>
    /// UTCID04 · B2=T, B3=F · Type A — selectedOptionIds null.
    /// Khẳng định toán tử ?? chặn được NullReferenceException.
    /// </summary>
    [Fact]
    public void UTCID04_NullSelection_ReturnsFalseWithoutThrowing()
    {
        var question = BuildQuestion(true, false, false);

        Assert.False(QuizGrading.IsAnswerCorrect(question, null!));
    }

    /// <summary>UTCID05 · B3=F · Type B — không chọn đáp án nào (bỏ trống câu hỏi).</summary>
    [Fact]
    public void UTCID05_EmptySelection_ReturnsFalse()
    {
        var question = BuildQuestion(true, false, false);

        Assert.False(QuizGrading.IsAnswerCorrect(question, Array.Empty<Guid>()));
    }

    /// <summary>UTCID06 · B4=F · Type A — câu nhiều đáp án đúng nhưng chọn THIẾU.</summary>
    [Fact]
    public void UTCID06_MultiChoiceMissingOneCorrectOption_ReturnsFalse()
    {
        var question = BuildQuestion(true, true, false);

        Assert.False(QuizGrading.IsAnswerCorrect(question, new[] { OptionAt(question, 0) }));
    }

    /// <summary>UTCID07 · B4=F · Type A — chọn THỪA một đáp án sai.</summary>
    [Fact]
    public void UTCID07_MultiChoiceWithExtraWrongOption_ReturnsFalse()
    {
        var question = BuildQuestion(true, true, false);

        Assert.False(QuizGrading.IsAnswerCorrect(question, new[]
        {
            OptionAt(question, 0), OptionAt(question, 1), OptionAt(question, 2)
        }));
    }

    /// <summary>UTCID08 · B4=T · Type N — chọn đúng tập, khác thứ tự (so sánh theo TẬP hợp).</summary>
    [Fact]
    public void UTCID08_MultiChoiceCorrectSetInDifferentOrder_ReturnsTrue()
    {
        var question = BuildQuestion(true, true, false);

        Assert.True(QuizGrading.IsAnswerCorrect(question, new[]
        {
            OptionAt(question, 1), OptionAt(question, 0)
        }));
    }

    /// <summary>UTCID09 · B4=T · Type B — client gửi trùng lựa chọn; ToHashSet khử trùng lặp.</summary>
    [Fact]
    public void UTCID09_DuplicatedSelection_IsDeduplicatedAndReturnsTrue()
    {
        var question = BuildQuestion(true, true, false);

        Assert.True(QuizGrading.IsAnswerCorrect(question, new[]
        {
            OptionAt(question, 0), OptionAt(question, 0), OptionAt(question, 1)
        }));
    }
}
