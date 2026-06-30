using EDCL.Module.Cargo.Application.Queries.GetManifestDetail;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Api.Controllers;

/// <summary>
/// Backend-for-Frontend (BFF) controller.
/// 
/// This controller lives in the Host layer and acts as an orchestrator —
/// it dispatches queries to multiple domain modules in parallel, merges
/// the results, and returns a single, frontend-optimized response.
/// 
/// Key benefit: domain modules (Job, Cargo) remain fully decoupled.
/// Neither module references the other. The Host layer is the only place
/// that knows about both, which is the correct place for cross-cutting concerns.
/// </summary>
[ApiController]
[Route("api/v1/bff")]
[Authorize(Roles = "ADMIN")]
public sealed class BffController(IMediator mediator, JobDbContext jobDbContext) : ControllerBase
{
    /// <summary>
    /// Returns a unified manifest detail by aggregating:
    ///   - Job Module    → scanning progress (TotalKanban, ScannedKanban, ScanStatus)
    ///   - Cargo Module  → master cargo data (Parts with PartNo, PartName, Qty, etc.)
    /// 
    /// Both queries are dispatched in parallel via Task.WhenAll for optimal latency.
    /// </summary>
    [HttpGet("manifests/{manifestNo}/detail")]
    public async Task<IActionResult> GetManifestDetail(string manifestNo, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        // ── Dispatch two domain queries in parallel ────────────────────────────
        var jobManifestTask    = jobDbContext.PickupOrderManifests
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(m => m.ManifestNo == manifestNo, cancellationToken);

        var cargoDetailTask    = mediator.Send(new GetManifestDetailQuery(manifestNo), cancellationToken);

        await Task.WhenAll(jobManifestTask, cargoDetailTask);

        var jobManifest       = await jobManifestTask;
        var cargoDetailResult = await cargoDetailTask;
        // ──────────────────────────────────────────────────────────────────────

        // Both not found → 404
        if (jobManifest is null && cargoDetailResult.IsFailure)
            return NotFound(ApiResponse<object>.Fail($"Manifest '{manifestNo}' not found.", traceId, 404));

        // Cargo not found → return partial data from Job module only
        if (cargoDetailResult.IsFailure)
        {
            return Ok(ApiResponse<BffManifestDetailDto>.Success(new BffManifestDetailDto(
                ManifestNo:    jobManifest!.ManifestNo,
                OrderType:     jobManifest.OrderType,
                DockCode:      jobManifest.DockCode,
                ScanStatus:    jobManifest.Status,
                TotalKanban:   jobManifest.TotalKanban,
                ScannedKanban: jobManifest.ScannedKanban,
                OrderNo:       null,
                PLaneNo:       null,
                Parts:         []
            ), traceId));
        }

        var cargo = cargoDetailResult.Value!;

        // Merge Job progress + Cargo master data ────────────────────────────────
        return Ok(ApiResponse<BffManifestDetailDto>.Success(new BffManifestDetailDto(
            ManifestNo:    manifestNo,
            OrderType:     jobManifest?.OrderType  ?? "ORG",
            DockCode:      cargo.DockCode,
            ScanStatus:    jobManifest?.Status     ?? "UNKNOWN",
            TotalKanban:   jobManifest?.TotalKanban    ?? cargo.TotalKanban,
            ScannedKanban: jobManifest?.ScannedKanban  ?? 0,
            OrderNo:       cargo.OrderNo,
            PLaneNo:       cargo.PLaneNo,
            Parts:         cargo.PartList.Select(p => new BffManifestPartDto(
                No:      p.No,
                PartNo:  p.PartNo,
                UniqNo:  p.UniqNo,
                PcsKbn:  p.PcsKbn,
                BoxType: p.BoxType,
                NoOfKbn: p.NoOfKbn
            )).ToList()
        ), traceId));
    }
}

// ── Response DTOs ─────────────────────────────────────────────────────────────
// Defined here (not in domain modules) intentionally — these are BFF contracts,
// shaped for the frontend, not domain objects.

public sealed record BffManifestDetailDto(
    string  ManifestNo,
    string  OrderType,
    string  DockCode,
    string  ScanStatus,
    int     TotalKanban,
    int     ScannedKanban,
    string? OrderNo,
    string? PLaneNo,
    List<BffManifestPartDto> Parts
);

public sealed record BffManifestPartDto(
    int    No,
    string PartNo,
    string UniqNo,
    int    PcsKbn,
    string BoxType,
    string NoOfKbn
);
