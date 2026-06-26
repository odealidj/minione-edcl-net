using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.CreateDriver;

public sealed record CreateDriverCommand(
    string Name,
    string Nik,
    string PhoneNumber,
    long? TransporterId = null) : IRequest<Result<CreateDriverResponse>>;

public sealed record CreateDriverResponse(
    long Id,
    string Name,
    string Nik,
    string PhoneNumber,
    long? TransporterId);
