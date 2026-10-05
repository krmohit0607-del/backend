using MediatR;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Exceptions;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Auth;

namespace MultiTenantSaaS.Application.Features.Auth.Commands;

public record LoginCommand(LoginRequestDto Dto) : IRequest<ApiResponse<AuthResponseDto>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindByEmailAsync(request.Dto.Email);
        if (user == null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Your account has been deactivated. Please contact your administrator.");
        }

        // If user belongs to a tenant, ensure tenant is active
        if (user.TenantId.HasValue)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId.Value, cancellationToken);
            if (tenant == null || !tenant.IsActive)
            {
                throw new UnauthorizedException("Your organization account is inactive. Please contact support.");
            }
        }

        var isPasswordValid = await _identityService.CheckPasswordAsync(user, request.Dto.Password);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var role = await _identityService.GetUserRoleAsync(user);
        System.Console.WriteLine($"[DEBUG] LoginCommandHandler - User: {user.Email}, TenantId: {user.TenantId?.ToString() ?? "NULL"}, HasValue: {user.TenantId.HasValue}");
        var accessToken = _tokenService.GenerateAccessToken(user, role);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, _currentUserService.IpAddress);

        user.LastLoginAt = DateTime.UtcNow;
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        string? companyName = null;
        if (user.TenantId.HasValue)
        {
            companyName = await _context.Tenants
                .Where(t => t.Id == user.TenantId.Value)
                .Select(t => t.CompanyName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        await _auditService.LogAsync(
            action: "Login",
            entityName: "ApplicationUser",
            entityId: user.Id.ToString(),
            tenantId: user.TenantId,
            userId: user.Id,
            cancellationToken: cancellationToken);

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role,
            TenantId = user.TenantId,
            CompanyName = companyName,
            AssignedVesselImo = user.AssignedVesselImo,
            AssignedVesselName = user.AssignedVesselName,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };

        return ApiResponse<AuthResponseDto>.SuccessResult(response, "Login successful.");
    }
}

public record RefreshTokenCommand(RefreshTokenRequestDto Dto) : IRequest<ApiResponse<AuthResponseDto>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IIdentityService identityService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tokenService = tokenService;
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.Dto.RefreshToken, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = existingToken.User;
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User account is inactive.");
        }

        // Revoke current token
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.RevokedByIp = _currentUserService.IpAddress;

        // Generate new tokens
        var role = await _identityService.GetUserRoleAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, role);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, _currentUserService.IpAddress);

        existingToken.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        string? companyName = null;
        if (user.TenantId.HasValue)
        {
            companyName = await _context.Tenants
                .Where(t => t.Id == user.TenantId.Value)
                .Select(t => t.CompanyName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role,
            TenantId = user.TenantId,
            CompanyName = companyName,
            AssignedVesselImo = user.AssignedVesselImo,
            AssignedVesselName = user.AssignedVesselName,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
        };

        return ApiResponse<AuthResponseDto>.SuccessResult(response, "Token refreshed successfully.");
    }
}

public record RevokeTokenCommand(string? RefreshToken) : IRequest<ApiResponse>;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, ApiResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RevokeTokenCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            // Revoke all active refresh tokens for the current user
            if (!_currentUserService.UserId.HasValue)
            {
                throw new UnauthorizedException();
            }

            var userTokens = await _context.RefreshTokens
                .Where(r => r.UserId == _currentUserService.UserId.Value && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var token in userTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = _currentUserService.IpAddress;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return ApiResponse.SuccessResult("All active sessions revoked.");
        }

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = _currentUserService.IpAddress;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse.SuccessResult("Token revoked successfully.");
    }
}
