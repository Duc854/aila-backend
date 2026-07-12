namespace AILA.Api.Controllers.Admin
{
    public record CreateTagRequest(string Name);

    public record UpdateTagRequest(string Name);

    public enum VerifyDecision
    {
        Approve,
        Reject
    }

    public record VerifyTagRequest(VerifyDecision Decision, string? RejectionReason);
}
