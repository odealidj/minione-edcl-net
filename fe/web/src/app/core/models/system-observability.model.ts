export interface SystemMetric {
  cpuUsagePct: number;
  memoryWorkingSetMb: number;
  gcTotalMemoryMb: number;
  threadPoolActiveThreads: number;
  threadPoolAvailableThreads: number;
  processUptime: string;
  processorCount: number;
}

export interface ResiliencyMetrics {
  idempotencySavedRequests: number;
  kanbansScannedTotal: number;
  cdcEventsProcessed: number;
  cdcEventsFailed: number;
  fcmNotificationsTotal: number;
}

export interface InfraHealthItem {
  name: string;
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  latencyMs: number;
  description: string;
}

export interface TimeSeriesData {
  labels: string[];
  cpuHistory: number[];
  memoryHistory: number[];
  requestRateHistory: number[];
}

export interface ObservabilityMetricsResponse {
  timestamp: string;
  system: SystemMetric;
  resiliency: ResiliencyMetrics;
  infraHealth: InfraHealthItem[];
  timeSeries: TimeSeriesData;
}
