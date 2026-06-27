using FluentValidation;

namespace EDCL.Module.Auth.Application.Commands.ChangeDriverPin;

public sealed class ChangeDriverPinCommandValidator : AbstractValidator<ChangeDriverPinCommand>
{
    public ChangeDriverPinCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Nomor HP wajib diisi.");

        RuleFor(x => x.OldPin)
            .NotEmpty().WithMessage("PIN lama wajib diisi.")
            .Length(6).WithMessage("PIN lama harus 6 digit.");

        RuleFor(x => x.NewPin)
            .NotEmpty().WithMessage("PIN baru wajib diisi.")
            .Length(6).WithMessage("PIN baru harus 6 digit.")
            .NotEqual(x => x.OldPin).WithMessage("PIN baru tidak boleh sama dengan PIN lama.");
    }
}
