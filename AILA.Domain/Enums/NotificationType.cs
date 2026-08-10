using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Enums
{
    public enum NotificationType
    {
        //Learner
        RegisterSuccessful,
        ResetPasswordSuccessful,
        PurchaseSubcriptionSuccesful,
        EnrollACourse,
        ReceiveExpertEvaluation,
        //Expert
        NewEvaluationRequest,
        CourseModerationResult,
        TagVerificationResult,
        //Admin
        NewContentReport,
        NewCourseReviewRequest,
        NewTagVerificationRequest,
    }
}
