namespace AILA.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("Bạn không có quyền truy cập tính năng này.") { }
}
