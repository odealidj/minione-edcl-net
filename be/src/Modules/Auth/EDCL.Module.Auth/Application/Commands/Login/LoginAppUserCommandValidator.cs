using FluentValidation;

namespace EDCL.Module.Auth.Application.Commands.Login;

public sealed class LoginAppUserCommandValidator : AbstractValidator<LoginAppUserCommand>
{
    public LoginAppUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
