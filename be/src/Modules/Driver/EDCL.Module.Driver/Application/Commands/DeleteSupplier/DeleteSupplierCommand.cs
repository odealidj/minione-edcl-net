using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.DeleteSupplier;

public sealed record DeleteSupplierCommand(long Id) : IRequest<Result>;
