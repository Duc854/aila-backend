using AILA.Application.Common.Interfaces;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Auth.Queries.GetCurrentUser
{
    public class GetCurrentUserQuery : IRequest<ResponseDto<CurrentUserResponse>>
    {
        public Guid UserId { get; set; }
    }

    public class CurrentUserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ResponseDto<CurrentUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
                return ResponseDto<CurrentUserResponse>.FailResult("USER_NOT_FOUND", "Không tìm thấy người dùng.");

            var response = new CurrentUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            };

            return ResponseDto<CurrentUserResponse>.SuccessResult(response);
        }
    }
}
