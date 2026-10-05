using FluentValidation;
using MultiTenantSaaS.Application.Features.Admin.Commands;

namespace MultiTenantSaaS.Application.Validators;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(v => v.Dto.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(v => v.Dto.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(v => v.Dto.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(v => v.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(v => v.Dto.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");
    }
}

public class SetEmployeePermissionsCommandValidator : AbstractValidator<SetEmployeePermissionsCommand>
{
    public SetEmployeePermissionsCommandValidator()
    {
        RuleFor(v => v.Dto.EmployeeUserId)
            .NotEmpty().WithMessage("EmployeeUserId is required.");

        RuleFor(v => v.Dto.Permissions)
            .NotNull().WithMessage("Permissions list is required.");
    }
}
