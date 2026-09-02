export interface SystemMetric {
  cpuUsagePct: number;
  memoryWorkingSetMb: number;
  gcTotalMemoryMb: number;
  threadPoolActiveThreads: number;
  threadPoolAvailableThreads: number;
  processUptime: string;
  processorCount: number;
}

export interface ServiceMemoryBreakdown {
  serviceName: string;
  processName: string;
  memoryMb: number;
  percentage: number;
  role: string;
  status: string;
  color: string;
}

export interface InfraSizingItem {
  componentName: string;
  category: string;
  memoryMb: number;
  percentage: number;
  role: string;
  color: string;
}

export interface HostCapacityGuide {
  totalClusterMemoryMb: number;
  totalFullStackMemoryMb: number;
  minDevVmRecommendation: string;
  prodVmRecommendation: string;
  multiServerRecommendation: string;
  infraBreakdown: InfraSizingItem[];
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
  memoryBreakdown: ServiceMemoryBreakdown[];
  hostCapacity?: HostCapacityGuide;
  resiliency: ResiliencyMetrics;
  infraHealth: InfraHealthItem[];
  timeSeries: TimeSeriesData;
}
