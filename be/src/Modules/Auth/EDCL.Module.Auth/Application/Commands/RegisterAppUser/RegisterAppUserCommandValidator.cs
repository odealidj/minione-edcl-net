using FluentValidation;

namespace EDCL.Module.Auth.Application.Commands.RegisterAppUser;

public sealed class RegisterAppUserCommandValidator : AbstractValidator<RegisterAppUserCommand>
{
    public RegisterAppUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(x => x.RoleCode)
            .NotEmpty().WithMessage("Role Code is required.")
            .MaximumLength(50).WithMessage("Role Code must not exceed 50 characters.");
    }
}
