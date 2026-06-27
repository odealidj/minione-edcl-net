using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;

namespace EDCL.Module.Auth.Application.Commands.ChangeDriverPin;

public sealed record ChangeDriverPinCommand(
    string PhoneNumber,
    string OldPin,
    string NewPin,
    string? DeviceInfo = null)
    : ICommand<Result<LoginResponse>>;
