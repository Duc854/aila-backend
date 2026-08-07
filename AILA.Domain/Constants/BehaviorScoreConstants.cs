using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Domain.Constants
{
    public static class BehaviorScoreConstants
    {
        // User đăng ký khóa học
        public const int EnrollCourse = 20;

        // User hoàn thành khóa học
        public const int CompleteCourse = 100;

        // User thực hành AI Practice
        public const int CompleteAIPractice = 30;

        // User đạt điểm tuyệt đối quiz
        public const int PerfectQuiz = 30;
    }
}
