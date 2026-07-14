using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Cargo.Application.Commands.ResolveManifestProblem;

public sealed record ResolveManifestProblemCommand(long Id, string Reason) : IRequest<Result<bool>>;
