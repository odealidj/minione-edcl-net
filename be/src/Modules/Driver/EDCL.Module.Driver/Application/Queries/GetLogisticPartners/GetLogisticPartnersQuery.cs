using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Domain;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetLogisticPartners;

public sealed record GetLogisticPartnersQuery(string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<GetLogisticPartnersResponse>>;

public sealed record GetLogisticPartnersResponse(
    System.Collections.Generic.IReadOnlyList<LogisticPartnerDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
