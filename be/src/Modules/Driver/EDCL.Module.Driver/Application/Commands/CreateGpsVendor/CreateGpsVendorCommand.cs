using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.CreateGpsVendor;

public sealed record CreateGpsVendorCommand(
    string Code,
    string Name,
    int ProviderType,
    string? ApiUrl,
    string? ApiUsername,
    string? ApiPassword,
    string? ApiToken) : IRequest<Result<long>>;
