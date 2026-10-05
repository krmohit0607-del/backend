namespace MultiTenantSaaS.Application.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? CompanyName { get; set; }
    public string? AssignedVesselImo { get; set; }
    public string? AssignedVesselName { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? CompanyName { get; set; }
    public string? AssignedVesselImo { get; set; }
    public string? AssignedVesselName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
