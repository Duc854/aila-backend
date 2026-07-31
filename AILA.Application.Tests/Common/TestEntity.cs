using System.Reflection;
using AILA.Domain.Common;

namespace AILA.Application.Tests.Common
{
    public static class TestEntity
    {
        /// <summary>
        /// Gán Id cố định cho entity trong test. <see cref="BaseEntity.Id"/> có setter protected
        /// nên chỉ chạm tới được qua reflection.
        /// CHỈ dùng khi test cần một Guid biết trước — mặc định hãy đọc entity.Id sau khi tạo.
        /// </summary>
        public static T SetId<T>(T entity, Guid id) where T : BaseEntity
        {
            typeof(BaseEntity)
                .GetProperty(nameof(BaseEntity.Id))!
                .GetSetMethod(nonPublic: true)!
                .Invoke(entity, new object[] { id });

            return entity;
        }

        /// <summary>
        /// Gán một property có setter non-public (navigation property của EF, ví dụ
        /// <c>Expert.User</c> / <c>Learner.User</c>). Unit test không có DbContext để EF tự
        /// nạp navigation, nên phải gắn tay.
        /// </summary>
        public static T SetProperty<T>(T target, string propertyName, object? value)
        {
            var prop = typeof(T).GetProperty(propertyName)
                ?? throw new ArgumentException($"Không tìm thấy property '{propertyName}' trên {typeof(T).Name}.");

            var setter = prop.GetSetMethod(nonPublic: true)
                ?? throw new ArgumentException($"Property '{propertyName}' không có setter.");

            setter.Invoke(target, new[] { value });

            return target;
        }
    }
}
