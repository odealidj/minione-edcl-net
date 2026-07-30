using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Driver.Application.Queries.TestGpsConnection;

public sealed record TestGpsConnectionQuery(
    int ProviderType,
    string? ApiUrl,
    string? ApiUsername,
    string? ApiPassword,
    string? ApiToken
) : IRequest<Result<bool>>;
