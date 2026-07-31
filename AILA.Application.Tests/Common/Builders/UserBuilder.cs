using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.Common.Builders
{
    /// <summary>
    /// Dựng <see cref="User"/> cho test. Chỉ khai báo thứ test quan tâm; phần còn lại là mặc định
    /// hợp lệ. Trạng thái Inactive đi qua đúng API công khai (<c>Deactivate()</c>), không set thẳng.
    /// </summary>
    public class UserBuilder
    {
        private string _email = "learner@aila.com";
        private string _fullName = "Nguyen An";
        private UserRole _role = UserRole.Learner;
        private string? _passwordHash = "$2a$HASH";
        private string? _googleId;
        private bool _inactive;
        private Guid? _id;

        public UserBuilder WithId(Guid id) { _id = id; return this; }
        public UserBuilder WithEmail(string email) { _email = email; return this; }
        public UserBuilder WithFullName(string fullName) { _fullName = fullName; return this; }
        public UserBuilder WithRole(UserRole role) { _role = role; return this; }
        public UserBuilder WithPasswordHash(string? hash) { _passwordHash = hash; return this; }
        public UserBuilder WithGoogleId(string? googleId) { _googleId = googleId; return this; }
        public UserBuilder WithActive(bool isActive) { _inactive = !isActive; return this; }
        public UserBuilder Inactive() { _inactive = true; return this; }

        public User Build()
        {
            var user = new User(_email, _fullName, _role, _passwordHash, _googleId);

            if (_inactive)
                user.Deactivate();

            if (_id.HasValue)
                TestEntity.SetId(user, _id.Value);

            return user;
        }
    }
}
