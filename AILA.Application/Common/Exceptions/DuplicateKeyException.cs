namespace AILA.Application.Common.Exceptions
{
    /// <summary>
    /// Vi phạm ràng buộc duy nhất ở tầng CSDL (hàng rào cuối chống race condition).
    /// Hạ tầng dịch lỗi provider-specific thành exception này để tầng Application
    /// không phải phụ thuộc vào EF Core / Npgsql.
    /// </summary>
    public class DuplicateKeyException : Exception
    {
        /// <summary>
        /// Tên unique index/constraint bị vi phạm, ví dụ "IX_SubscriptionPlans_Name".
        /// Có thể rỗng nếu provider không cung cấp.
        /// </summary>
        public string ConstraintName { get; }

        public DuplicateKeyException(string constraintName, Exception? innerException = null)
            : base($"Vi phạm ràng buộc duy nhất: {constraintName}", innerException)
        {
            ConstraintName = constraintName;
        }
    }
}
