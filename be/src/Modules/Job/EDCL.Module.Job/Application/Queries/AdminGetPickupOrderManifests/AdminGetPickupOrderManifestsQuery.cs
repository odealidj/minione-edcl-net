using EDCL.Shared.Kernel.Common;
using MediatR;
using System.Collections.Generic;
using EDCL.Module.Job.Application.Queries.AdminGetPickupOrderById;

namespace EDCL.Module.Job.Application.Queries.AdminGetPickupOrderManifests;

public sealed record AdminGetPickupOrderManifestsQuery(long PickupOrderId, long StopId, int PageNumber = 1, int PageSize = 30) 
    : IRequest<Result<AdminGetPickupOrderManifestsResponse>>;

public sealed record AdminGetPickupOrderManifestsResponse(
    IReadOnlyList<AdminPickupOrderManifestDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
