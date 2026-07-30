using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Driver.Application.Queries.TestGpsConnection;

public sealed record TestGpsConnectionQuery(
    long Id
) : IRequest<Result<bool>>;
