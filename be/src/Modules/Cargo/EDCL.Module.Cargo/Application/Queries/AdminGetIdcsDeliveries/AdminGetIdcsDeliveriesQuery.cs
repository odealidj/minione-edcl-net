using EDCL.Shared.Http.Responses;
using EDCL.Module.Cargo.Application.DTOs;
using MediatR;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetIdcsDeliveries;

public record AdminGetIdcsDeliveriesQuery(
    string? SearchQuery,
    int Page = 1,
    int PageSize = 10
) : IRequest<ApiResponse<List<IdcsDeliveryDto>>>;
