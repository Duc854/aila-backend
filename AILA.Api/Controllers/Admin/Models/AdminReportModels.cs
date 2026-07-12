namespace AILA.Api.Controllers.Admin
{
    public enum ModerationAction
    {
        DismissReport,
        RemoveContent,
        WarnUser,
        SuspendUser
    }

    public record ReviewReportRequest(ModerationAction Action, string? ResolutionNote);
}
