namespace AILA.Application.Features.SubscriptionPlans
{
    /// <summary>
    /// Mã lỗi dùng chung cho UC-09/UC-90/UC-91/UC-92. Controller map các mã này sang HTTP status.
    /// </summary>
    public static class SubscriptionPlanErrors
    {
        public const string NameRequired = "PLAN_NAME_REQUIRED";
        public const string NameTooLong = "PLAN_NAME_TOO_LONG";
        public const string DescriptionTooLong = "PLAN_DESCRIPTION_TOO_LONG";
        public const string InvalidPrice = "INVALID_PLAN_PRICE";
        public const string InvalidTierLevel = "INVALID_TIER_LEVEL";
        public const string InvalidDuration = "INVALID_PLAN_DURATION";
        public const string InvalidAiTokenLimit = "INVALID_AI_TOKEN_LIMIT";
        public const string InvalidAiPracticeScenarioLimit = "INVALID_AI_PRACTICE_SCENARIO_LIMIT";
        public const string InvalidExpertEvaluationLimit = "INVALID_EXPERT_EVALUATION_LIMIT";
        public const string InvalidDisplayOrder = "INVALID_DISPLAY_ORDER";

        public const string NameAlreadyExists = "PLAN_NAME_ALREADY_EXISTS";
        public const string TierLevelAlreadyExists = "TIER_LEVEL_ALREADY_EXISTS";

        public const string NotFound = "PLAN_NOT_FOUND";
        public const string NotAvailable = "PLAN_NOT_AVAILABLE";
        public const string AlreadyActive = "PLAN_ALREADY_ACTIVE";
        public const string AlreadyInactive = "PLAN_ALREADY_INACTIVE";

        public const string ValidationError = "VALIDATION_ERROR";
    }
}
