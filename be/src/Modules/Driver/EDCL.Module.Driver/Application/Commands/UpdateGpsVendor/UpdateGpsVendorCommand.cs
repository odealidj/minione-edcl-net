using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.UpdateGpsVendor;

public sealed record UpdateGpsVendorCommand(
    long Id,
    string Code,
    string Name,
    int ProviderType,
    string? ApiUrl,
    string? ApiUsername,
    string? ApiPassword,
    string? ApiToken) : IRequest<Result>;
