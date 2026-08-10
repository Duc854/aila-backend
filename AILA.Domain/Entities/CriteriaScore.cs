namespace AILA.Domain.Entities;
using AILA.Domain.Common;
public class CriteriaScore : BaseEntity {
    public Guid SubmissionId { get; private set; }
    public Guid CriteriaId { get; private set; }
    public decimal Score { get; private set; }
    public string Feedback { get; private set; } = string.Empty;
    private CriteriaScore() { }
    public CriteriaScore(Guid submissionId, Guid criteriaId, decimal score, string feedback) {
        Id = Guid.NewGuid();
        SubmissionId = submissionId;
        CriteriaId = criteriaId;
        Score = score;
        Feedback = feedback;
    }
}
