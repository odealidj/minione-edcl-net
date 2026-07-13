namespace EDCL.Module.Cargo.Application.Queries.GetPendingManifestSuppliers;

using EDCL.Shared.Kernel.Common;
using MediatR;
using System.Collections.Generic;

public record PendingManifestSupplierDto(string SupplierCode, string SupplierName);

public sealed record GetPendingManifestSuppliersQuery() : IRequest<Result<IReadOnlyList<PendingManifestSupplierDto>>>;
