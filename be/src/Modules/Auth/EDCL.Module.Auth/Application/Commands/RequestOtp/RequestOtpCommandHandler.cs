using EDCL.Module.Auth.Application.Ports;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Auth.Application.Commands.RequestOtp;

public sealed class RequestOtpCommandHandler(
    IDriverRepository driverRepository,
    ICachePort cachePort,
    ILogger<RequestOtpCommandHandler> logger)
    : IRequestHandler<RequestOtpCommand, Result<RequestOtpResponse>>
{
    public async Task<Result<RequestOtpResponse>> Handle(
        RequestOtpCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify that the driver exists and is active
        var driver = await driverRepository.FindActiveByPhoneAsync(
            request.PhoneNumber, cancellationToken);

        if (driver is null)
        {
            // For security, it's better not to leak whether the phone number exists or not.
            // But usually in internal apps, it's fine.
            return Error.NotFound("Driver", request.PhoneNumber);
        }

        // 2. Generate a random 4-digit PIN
        // In a real production system, use RandomNumberGenerator for cryptographically secure random.
        var random = new Random();
        var pin = random.Next(1000, 9999).ToString();

        // 3. Save PIN in Cache (Redis)
        var cacheKey = CacheKeys.OtpVerification(request.PhoneNumber);
        await cachePort.SetAsync(cacheKey, pin, CacheTtl.OtpVerification, cancellationToken);

        // 4. Log the PIN (Simulating sending an SMS/WhatsApp)
        // [MOCK SMS PROVIDER]
        logger.LogWarning(
            "===============================================================\n" +
            "MOCK SMS/WA/FCM: \n" +
            "To: {Phone}\n" +
            "Message: {Pin} adalah kode rahasia EDCL Anda. " +
            "Berlaku untuk 3 menit. Jangan berikan kode ini kepada siapapun.\n" +
            "===============================================================",
            request.PhoneNumber, pin);

        return new RequestOtpResponse(
            Success: true,
            Message: "OTP has been sent successfully.");
    }
}
