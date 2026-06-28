using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Cargo.Application.Queries.GetManifestDetail;

public sealed class GetManifestDetailQueryHandler(CargoDbContext dbContext)
    : IRequestHandler<GetManifestDetailQuery, Result<ManifestDetailDto>>
{
    public async Task<Result<ManifestDetailDto>> Handle(GetManifestDetailQuery request, CancellationToken cancellationToken)
    {
        var manifest = await dbContext.Manifests
            .Include(m => m.Parts)
            .Include(m => m.Kanbans)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ManifestNo == request.ManifestNo, cancellationToken);

        if (manifest is null)
        {
            return Result<ManifestDetailDto>.Failure(Error.NotFound("Manifest.NotFound", $"Manifest with number {request.ManifestNo} not found."));
        }

        var partDtos = new List<ManifestPartDto>();
        int no = 1;

        // Group Kanbans by PartNo to get the count of Kanbans per part
        var kanbanCounts = manifest.Kanbans
            .GroupBy(k => k.PartNo)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var part in manifest.Parts)
        {
            var kanbanCountForPart = kanbanCounts.GetValueOrDefault(part.PartNo, 0);
            
            // Format "No. Of Kbn" e.g., "2/2" or "5/5"
            // For now, since we only read from Master data, we assume all kanbans are required (e.g. kanbanCountForPart / kanbanCountForPart)
            string noOfKbnStr = $"{kanbanCountForPart}/{kanbanCountForPart}";

            partDtos.Add(new ManifestPartDto(
                No: no++,
                PartNo: part.PartNo,
                UniqNo: part.UniqNo ?? "-",
                PcsKbn: part.Qty,
                BoxType: part.BoxType ?? "-",
                NoOfKbn: noOfKbnStr
            ));
        }

        var dto = new ManifestDetailDto(
            RouteCycle: $"- {manifest.Cycle}", // Cargo module doesn't have Route, only Cycle
            DeliveryNo: "-", // Delivery No is managed by Job Module, UI should retain this from previous screen
            ManifestNo: manifest.ManifestNo,
            TotalKanban: manifest.Kanbans.Count,
            OrderNo: manifest.OrderNo,
            DockCode: manifest.DockCode,
            PLaneNo: manifest.PLaneNo,
            PartList: partDtos
        );

        return Result<ManifestDetailDto>.Success(dto);
    }
}
