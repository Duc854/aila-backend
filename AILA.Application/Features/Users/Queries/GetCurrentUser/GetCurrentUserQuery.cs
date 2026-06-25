using AILA.Application.Common.Interfaces;
using AILA.Application.Features.Users.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.Users.Queries.GetCurrentUser
{
    public class GetCurrentUserQuery : IRequest<ResponseDto<CurrentUserDto>>
    {
        public Guid UserId { get; set; }
    }

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ResponseDto<CurrentUserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDto<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
                return ResponseDto<CurrentUserDto>.FailResult("USER_NOT_FOUND", "Không tìm thấy người dùng.");

            var response = new CurrentUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            };

            return ResponseDto<CurrentUserDto>.SuccessResult(response);
        }
    }
}
