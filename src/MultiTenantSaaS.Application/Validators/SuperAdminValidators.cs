using FluentValidation;
using MultiTenantSaaS.Application.Features.SuperAdmin.Commands;

namespace MultiTenantSaaS.Application.Validators;

public class CreateAdminCommandValidator : AbstractValidator<CreateAdminCommand>
{
    public CreateAdminCommandValidator()
    {
        RuleFor(v => v.Dto.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(150).WithMessage("Company name must not exceed 150 characters.");

        RuleFor(v => v.Dto.AdminFullName)
            .NotEmpty().WithMessage("Admin full name is required.")
            .MaximumLength(100).WithMessage("Admin full name must not exceed 100 characters.");

        RuleFor(v => v.Dto.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(v => v.Dto.AdminPassword)
            .NotEmpty().WithMessage("Admin password is required.")
            .MinimumLength(8).WithMessage("Admin password must be at least 8 characters.");
    }
}

public class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
{
    public CreateModuleCommandValidator()
    {
        RuleFor(v => v.Dto.Name)
            .NotEmpty().WithMessage("Module name is required.")
            .MaximumLength(100).WithMessage("Module name must not exceed 100 characters.");
    }
}

public class SetTenantModuleAccessCommandValidator : AbstractValidator<SetTenantModuleAccessCommand>
{
    public SetTenantModuleAccessCommandValidator()
    {
        RuleFor(v => v.Dto.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.Dto.ModuleId)
            .NotEmpty().WithMessage("ModuleId is required.");
    }
}
