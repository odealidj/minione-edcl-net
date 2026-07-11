using EDCL.Shared.Kernel.Common;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;
using System.Collections.Generic;

namespace EDCL.Module.Driver.Application.Queries.GetTruckAssignments;

public sealed record GetTruckAssignmentsQuery() : IRequest<Result<IReadOnlyList<TruckDriverAssignmentDto>>>;
