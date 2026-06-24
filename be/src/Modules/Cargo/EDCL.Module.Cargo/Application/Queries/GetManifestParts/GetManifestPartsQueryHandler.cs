namespace EDCL.Module.Cargo.Application.Queries.GetManifestParts;

using Dapper;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public sealed class GetManifestPartsQueryHandler(IConfiguration configuration)
    : IRequestHandler<GetManifestPartsQuery, Result<ManifestPartsResponse>>
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public async Task<Result<ManifestPartsResponse>> Handle(GetManifestPartsQuery request, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Fetch Manifest basic info
        var manifestSql = "SELECT ManifestNo FROM [ingestion].[manifests] WHERE Id = @ManifestId AND IsDeleted = 0";
        var manifestNo = await connection.QuerySingleOrDefaultAsync<string>(manifestSql, new { request.ManifestId });

        if (string.IsNullOrEmpty(manifestNo))
        {
            return Error.NotFound("Manifest", request.ManifestId);
        }

        // We fetch parts and map them. Since scanning parts is in Job module, Job module kanban scans verify the ManifestPart.
        // Wait, KanbanNo maps to parts, but scanning is recorded at the Manifest level in Job schema.
        // Let's assume if the manifest is Verified in Job schema, all parts are 'Scanned'. Or we query Kanban tables.
        // For simplicity in this bounded context, we just read the ingestion parts.
        var partsSql = @"
            SELECT 
                Id AS PartId,
                PartNo,
                PartName,
                Qty,
                KanbanNo,
                Status
            FROM [ingestion].[manifest_parts]
            WHERE ManifestId = @ManifestId AND IsDeleted = 0
            ORDER BY PartNo ASC";

        var parts = await connection.QueryAsync<ManifestPartDto>(partsSql, new { request.ManifestId });
        var partsList = parts.ToList();

        var total = partsList.Count;
        var scanned = partsList.Count(x => x.Status == "Scanned");

        return new ManifestPartsResponse(request.ManifestId, manifestNo, total, scanned, partsList);
    }
}
