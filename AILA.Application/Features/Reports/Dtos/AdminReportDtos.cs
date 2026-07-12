namespace AILA.Application.Features.Reports.Dtos
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
