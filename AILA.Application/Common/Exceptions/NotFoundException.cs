namespace AILA.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Không tìm thấy dữ liệu '{name}' ({key}).") { }
}
