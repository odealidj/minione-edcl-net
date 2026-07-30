using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.DeleteGpsVendor;

public sealed record DeleteGpsVendorCommand(long Id) : IRequest<Result>;
