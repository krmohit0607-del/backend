using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Auth;

namespace MultiTenantSaaS.Application.Features.Auth.Queries;

public record GetCurrentUserQuery : IRequest<ApiResponse<UserProfileDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly IMapper _mapper;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", _currentUserService.UserId.Value);
        }

        var role = await _identityService.GetUserRoleAsync(user);
        var dto = _mapper.Map<UserProfileDto>(user);
        dto.Role = role;

        return ApiResponse<UserProfileDto>.SuccessResult(dto);
    }
}
