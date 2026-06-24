namespace EDCL.Module.Cargo.Application.Queries.GetManifests;

using Dapper;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public sealed class GetManifestsQueryHandler(IConfiguration configuration)
    : IRequestHandler<GetManifestsQuery, Result<ManifestsResponse>>
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public async Task<Result<ManifestsResponse>> Handle(GetManifestsQuery request, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT 
                m.Id AS ManifestId,
                m.ManifestNo,
                m.SupplierCode,
                m.SupplierName,
                m.Sequence,
                m.Cycle,
                m.OrderType,
                m.Status AS IngestionStatus,
                pom.Status AS JobStatus
            FROM [ingestion].[manifests] m
            INNER JOIN [job].[pickup_order_manifests] pom ON m.Id = pom.ManifestId
            WHERE pom.PickupOrderDetailId = @StopId AND m.OrderType = 'ORG' AND m.IsDeleted = 0
            ORDER BY m.Sequence ASC";

        var manifests = await connection.QueryAsync<ManifestDto>(sql, new { request.StopId });
        var manifestList = manifests.ToList();

        var total = manifestList.Count;
        var scanned = manifestList.Count(x => x.JobStatus == "Verified"); // Match enum value in Job module

        return new ManifestsResponse(request.StopId, total, scanned, manifestList);
    }
}
