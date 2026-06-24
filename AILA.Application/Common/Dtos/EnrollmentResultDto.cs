namespace AILA.Application.Common.Dtos
{
    public class EnrollmentResultDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid LearnerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
    }
}
