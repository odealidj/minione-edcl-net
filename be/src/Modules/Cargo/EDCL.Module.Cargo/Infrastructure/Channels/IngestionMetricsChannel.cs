namespace EDCL.Module.Cargo.Infrastructure.Channels;

using System;
using EDCL.Module.Cargo.Domain.Events;

public class IngestionMetricsChannel
{
    public event Action<IngestionMetricsEvent>? OnMetricsReceived;

    public void BroadcastMetrics(IngestionMetricsEvent metricsEvent)
    {
        OnMetricsReceived?.Invoke(metricsEvent);
    }
}
