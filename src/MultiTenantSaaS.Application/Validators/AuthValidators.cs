using FluentValidation;
using MultiTenantSaaS.Application.DTOs.Auth;
using MultiTenantSaaS.Application.Features.Auth.Commands;

namespace MultiTenantSaaS.Application.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(v => v.Dto.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(v => v.Dto.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(v => v.Dto.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
