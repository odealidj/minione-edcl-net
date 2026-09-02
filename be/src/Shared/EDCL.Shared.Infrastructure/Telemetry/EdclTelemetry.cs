using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EDCL.Shared.Infrastructure.Telemetry;

/// <summary>
/// Centralized Telemetry definitions (ActivitySource for Tracing and Meter for Metrics) for EDCL.
/// </summary>
public static class EdclTelemetry
{
    public const string ServiceName = "EDCL.Api";
    public const string ServiceVersion = "1.0.0";

    // --- Distributed Tracing ActivitySource ---
    public const string ActivitySourceName = "EDCL.Tracing";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, ServiceVersion);

    // --- Metrics Meter ---
    public const string MeterName = "EDCL.Logistics";
    public static readonly Meter Meter = new(MeterName, ServiceVersion);

    // --- Domain / Logistics Metrics ---
    public static readonly Counter<long> KanbanScannedCounter = 
        Meter.CreateCounter<long>("edcl.kanban.scanned.total", "items", "Total kanban barcodes scanned by drivers");

    public static readonly Histogram<double> PickupOrderDurationHistogram = 
        Meter.CreateHistogram<double>("edcl.pickup_order.duration.seconds", "s", "Duration of pickup order from Start to Completion");

    public static readonly Counter<long> CdcEventsProcessedCounter = 
        Meter.CreateCounter<long>("edcl.cdc.events.processed.total", "events", "Total CDC events processed by ingestion worker");

    public static readonly Counter<long> CdcEventsFailedCounter = 
        Meter.CreateCounter<long>("edcl.cdc.events.failed.total", "events", "Total CDC events failed or redirected to retry/DLQ");

    public static readonly Counter<long> IdempotencyReplayedCounter = 
        Meter.CreateCounter<long>("edcl.idempotency.replayed.total", "requests", "Total duplicate requests intercepted by Redis Idempotency");

    public static readonly Counter<long> FcmNotificationsCounter = 
        Meter.CreateCounter<long>("edcl.fcm.notifications.total", "notifications", "Total push notifications sent to drivers");
}
