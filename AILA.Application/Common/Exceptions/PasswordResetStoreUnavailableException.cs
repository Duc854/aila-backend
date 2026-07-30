namespace AILA.Application.Common.Exceptions
{
    /// <summary>
    /// EDGE-13: store giữ state reset (Redis) không sẵn sàng. Handler bắt exception này và
    /// trả lỗi 503 chung — <b>không</b> cho luồng reset đi tiếp khi không xác thực được OTP.
    /// </summary>
    public sealed class PasswordResetStoreUnavailableException : Exception
    {
        public PasswordResetStoreUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
