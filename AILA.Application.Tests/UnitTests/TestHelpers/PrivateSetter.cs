using System.Reflection;

namespace AILA.Application.Tests.UnitTests.TestHelpers;

/// <summary>
/// Đặt giá trị cho các thuộc tính domain có <c>private set</c> (được gán trong constructor).
///
/// White-box test cần dựng chính xác trạng thái đầu vào của từng đường thực thi
/// (ví dụ: quota đã dùng hết từ hôm qua, lượt làm bài bắt đầu 60 phút trước).
/// Dùng reflection ở tầng test để KHÔNG phải nới lỏng encapsulation của production code.
///
/// Xem Test Design §0.3 — danh sách thuộc tính được phép đặt bằng cách này.
/// </summary>
internal static class PrivateSetter
{
    public static T Set<T>(T target, string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(target);

        var setter = FindProperty(target.GetType(), propertyName)?.GetSetMethod(nonPublic: true);
        if (setter is not null)
        {
            setter.Invoke(target, new[] { value });
            return target;
        }

        var field = FindField(target.GetType(), $"<{propertyName}>k__BackingField")
                    ?? FindField(target.GetType(), propertyName);

        if (field is null)
        {
            throw new InvalidOperationException(
                $"Không tìm thấy setter hoặc backing field cho '{propertyName}' trên {target.GetType().Name}.");
        }

        field.SetValue(target, value);
        return target;
    }

    private static PropertyInfo? FindProperty(Type? type, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (var current = type; current is not null; current = current.BaseType)
        {
            var property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
            if (property is not null)
                return property;
        }

        return null;
    }

    private static FieldInfo? FindField(Type? type, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (var current = type; current is not null; current = current.BaseType)
        {
            var field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
            if (field is not null)
                return field;
        }

        return null;
    }
}
