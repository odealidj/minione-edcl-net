using FluentValidation;

namespace EDCL.Module.Auth.Application.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithErrorCode("PhoneNumber.Required")
            .WithMessage("Nomor HP tidak boleh kosong.")
            .Matches(@"^(0|\+62|62)[0-9]{8,12}$")
            .WithErrorCode("PhoneNumber.InvalidFormat")
            .WithMessage("Format nomor HP tidak valid. Contoh: 08123456789 atau +628123456789.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithErrorCode("Pin.Required")
            .WithMessage("PIN tidak boleh kosong.")
            .Length(4, 8).WithErrorCode("Pin.InvalidLength")
            .WithMessage("PIN harus terdiri dari 4–8 digit angka.")
            .Matches(@"^\d+$").WithErrorCode("Pin.NumericOnly")
            .WithMessage("PIN hanya boleh berisi angka.");
    }
}
