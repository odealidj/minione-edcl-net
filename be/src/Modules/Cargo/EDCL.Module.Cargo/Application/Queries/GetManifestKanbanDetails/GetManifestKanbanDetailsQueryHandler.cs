namespace EDCL.Module.Cargo.Application.Queries.GetManifestKanbanDetails;

using Dapper;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public sealed class GetManifestKanbanDetailsQueryHandler(IConfiguration configuration)
    : IRequestHandler<GetManifestKanbanDetailsQuery, Result<PaginatedResult<ManifestKanbanDto>>>
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public async Task<Result<PaginatedResult<ManifestKanbanDto>>> Handle(GetManifestKanbanDetailsQuery request, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Check if manifest exists
        var manifestExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT CAST(1 AS BIT) FROM [ingestion].[manifests] WHERE Id = @ManifestId AND IsDeleted = 0",
            new { request.ManifestId });

        if (!manifestExists)
        {
            return Error.NotFound("Manifest", request.ManifestId);
        }

        // Count total kanbans for this manifest
        var countSql = @"
            SELECT COUNT(1) 
            FROM [ingestion].[manifest_kanbans] 
            WHERE ManifestId = @ManifestId AND IsDeleted = 0";
            
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { request.ManifestId });

        // Fetch paginated kanban joined with part
        var sql = @"
            SELECT 
                k.KanbanCd,
                k.PartNo,
                p.PartName,
                p.Qty AS PartQty
            FROM [ingestion].[manifest_kanbans] k
            LEFT JOIN [ingestion].[manifest_parts] p 
                ON k.ManifestId = p.ManifestId 
                AND k.PartNo = p.PartNo 
                AND p.IsDeleted = 0
            WHERE k.ManifestId = @ManifestId 
              AND k.IsDeleted = 0
            ORDER BY k.Id ASC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var offset = (request.PageNumber - 1) * request.PageSize;

        var items = await connection.QueryAsync<ManifestKanbanDto>(sql, new 
        { 
            request.ManifestId, 
            Offset = offset, 
            request.PageSize 
        });

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var result = new PaginatedResult<ManifestKanbanDto>(
            items.ToList().AsReadOnly(),
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages);

        return result;
    }
}
