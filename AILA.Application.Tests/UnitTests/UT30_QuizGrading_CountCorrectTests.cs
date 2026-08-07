using AILA.Application.Features.Quizzes;
using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.UnitTests;

/// <summary>
/// Sheet: UT30_CountCorrect — <see cref="QuizGrading.CountCorrect"/>
/// Module: Quiz · CC = 4 · 6 test case
///
/// Nhánh: B1 = vòng lặp trên questionsById.Values · B2 = TryGetValue không tìm thấy lựa chọn
///        B3 = toán tử ?? (selected ?? Empty) · B4 = IsAnswerCorrect trả true
///
/// Điểm nghiệp vụ: hàm duyệt theo questionsById (đề bài), KHÔNG duyệt theo bài nộp
/// ⇒ client gửi thừa câu hỏi lạ cũng không làm sai số câu đúng.
/// </summary>
public class UT30_QuizGrading_CountCorrectTests
{
    private static readonly Guid QuizMaterialId = Guid.NewGuid();

    /// <summary>Tạo n câu hỏi, mỗi câu 2 đáp án (index 0 đúng, index 1 sai).</summary>
    private static List<Question> BuildQuestions(int count)
    {
        var questions = new List<Question>();
        for (var i = 0; i < count; i++)
        {
            var q = new Question(QuizMaterialId, $"Câu hỏi {i + 1}", QuestionType.SingleChoice, i + 1);
            q.AddAnswerOption(new AnswerOption(q.Id, "Đáp án A", true, 1));
            q.AddAnswerOption(new AnswerOption(q.Id, "Đáp án B", false, 2));
            questions.Add(q);
        }
        return questions;
    }

    private static Dictionary<Guid, Question> ById(List<Question> questions) =>
        questions.ToDictionary(q => q.Id);

    private static Guid CorrectOf(Question q) => q.AnswerOptions.First(o => o.IsCorrect).Id;
    private static Guid WrongOf(Question q) => q.AnswerOptions.First(o => !o.IsCorrect).Id;

    /// <summary>UTCID01 · B4=T 2 lần · Type N — 3 câu, đúng 2.</summary>
    [Fact]
    public void UTCID01_TwoOfThreeCorrect_ReturnsTwo()
    {
        var questions = BuildQuestions(3);
        var selections = new Dictionary<Guid, List<Guid>>
        {
            [questions[0].Id] = new() { CorrectOf(questions[0]) },
            [questions[1].Id] = new() { CorrectOf(questions[1]) },
            [questions[2].Id] = new() { WrongOf(questions[2]) }
        };

        Assert.Equal(2, QuizGrading.CountCorrect(ById(questions), selections));
    }

    /// <summary>UTCID02 · B1 = 0 vòng lặp · Type B — đề bài rỗng.</summary>
    [Fact]
    public void UTCID02_NoQuestions_ReturnsZero()
    {
        var result = QuizGrading.CountCorrect(
            new Dictionary<Guid, Question>(),
            new Dictionary<Guid, List<Guid>>());

        Assert.Equal(0, result);
    }

    /// <summary>UTCID03 · B2=F (TryGetValue miss), B3 (??) · Type B — bỏ trống hoàn toàn.</summary>
    [Fact]
    public void UTCID03_NoSelectionForQuestion_CountsAsWrong()
    {
        var questions = BuildQuestions(1);

        var result = QuizGrading.CountCorrect(ById(questions), new Dictionary<Guid, List<Guid>>());

        Assert.Equal(0, result);
    }

    /// <summary>UTCID04 · B4=T toàn bộ · Type B — đúng hết (biên trên).</summary>
    [Fact]
    public void UTCID04_AllCorrect_ReturnsQuestionCount()
    {
        var questions = BuildQuestions(3);
        var selections = questions.ToDictionary(q => q.Id, q => new List<Guid> { CorrectOf(q) });

        Assert.Equal(3, QuizGrading.CountCorrect(ById(questions), selections));
    }

    /// <summary>UTCID05 · B4=F toàn bộ · Type A — sai hết (biên dưới).</summary>
    [Fact]
    public void UTCID05_AllWrong_ReturnsZero()
    {
        var questions = BuildQuestions(3);
        var selections = questions.ToDictionary(q => q.Id, q => new List<Guid> { WrongOf(q) });

        Assert.Equal(0, QuizGrading.CountCorrect(ById(questions), selections));
    }

    /// <summary>
    /// UTCID06 · B1 duyệt theo đề bài · Type A — bài nộp chứa QuestionId lạ.
    /// Câu lạ bị bỏ qua vì vòng lặp chạy trên questionsById, không phải trên selections.
    /// </summary>
    [Fact]
    public void UTCID06_SelectionsWithUnknownQuestion_AreIgnored()
    {
        var questions = BuildQuestions(2);
        var selections = new Dictionary<Guid, List<Guid>>
        {
            [questions[0].Id] = new() { CorrectOf(questions[0]) },
            [questions[1].Id] = new() { CorrectOf(questions[1]) },
            [Guid.NewGuid()] = new() { Guid.NewGuid() }
        };

        Assert.Equal(2, QuizGrading.CountCorrect(ById(questions), selections));
    }
}
