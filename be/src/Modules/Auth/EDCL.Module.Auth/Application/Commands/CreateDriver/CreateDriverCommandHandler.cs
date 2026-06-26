using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Auth.Application.Commands.CreateDriver;

public sealed class CreateDriverCommandHandler(
    IDriverRepository driverRepository,
    ILogger<CreateDriverCommandHandler> logger)
    : IRequestHandler<CreateDriverCommand, Result<CreateDriverResponse>>
{
    public async Task<Result<CreateDriverResponse>> Handle(
        CreateDriverCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify NIK is unique
        var existingByNik = await driverRepository.FindByNikAsync(request.Nik, cancellationToken);
        if (existingByNik is not null)
        {
            return Error.Conflict("Driver.NikInUse", "The provided NIK is already registered.");
        }

        // 2. Verify Phone Number is unique among active drivers
        var existingByPhone = await driverRepository.FindActiveByPhoneAsync(request.PhoneNumber, cancellationToken);
        if (existingByPhone is not null)
        {
            return Error.Conflict("Driver.PhoneInUse", "The provided phone number is already active for another driver.");
        }

        // 3. Define Default PIN and Hash it
        const string DefaultPin = "123456"; // Default standard PIN
        var pinHash = BCrypt.Net.BCrypt.HashPassword(DefaultPin);

        // 4. Create Driver Entity
        var driver = Driver.Create(
            name: request.Name,
            nik: request.Nik,
            phoneNumber: request.PhoneNumber,
            pinHash: pinHash,
            transporterId: request.TransporterId);

        // 5. Save to DB
        await driverRepository.AddAsync(driver, cancellationToken);
        await driverRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Driver {DriverId} ({Name}) registered successfully with default PIN.", driver.Id, driver.Name);

        return new CreateDriverResponse(
            Id: driver.Id,
            Name: driver.Name,
            Nik: driver.Nik,
            PhoneNumber: driver.PhoneNumber,
            TransporterId: driver.TransporterId);
    }
}
