import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SystemObservabilityService } from '../../../core/services/system-observability.service';
import { ObservabilityMetricsResponse } from '../../../core/models/system-observability.model';
import { Chart, registerables } from 'chart.js/auto';

Chart.register(...registerables);

@Component({
  selector: 'app-system-observability',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './system-observability.html'
})
export class SystemObservabilityComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('cpuMemCanvas') cpuMemCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('throughputCanvas') throughputCanvas!: ElementRef<HTMLCanvasElement>;

  metrics = signal<ObservabilityMetricsResponse | null>(null);
  isLoading = signal<boolean>(true);
  isRefreshing = signal<boolean>(false);
  lastUpdated = signal<string>('');
  
  refreshIntervalSeconds = signal<number>(5);
  private timerHandle: any = null;

  private cpuMemChart: Chart | null = null;
  private throughputChart: Chart | null = null;

  constructor(private observabilityService: SystemObservabilityService) {}

  ngOnInit(): void {
    this.fetchMetrics();
    this.startAutoRefresh();
  }

  ngAfterViewInit(): void {
    // Initial chart creation will happen when first metrics arrive
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
    if (this.cpuMemChart) this.cpuMemChart.destroy();
    if (this.throughputChart) this.throughputChart.destroy();
  }

  setRefreshInterval(seconds: number): void {
    this.refreshIntervalSeconds.set(seconds);
    this.stopAutoRefresh();
    if (seconds > 0) {
      this.startAutoRefresh();
    }
  }

  fetchMetrics(manual = false): void {
    if (manual) this.isRefreshing.set(true);

    this.observabilityService.getMetrics().subscribe({
      next: (res) => {
        if (res.status === 'success' && res.data) {
          this.metrics.set(res.data);
          this.lastUpdated.set(new Date().toLocaleTimeString());
          this.updateCharts(res.data);
        }
        this.isLoading.set(false);
        this.isRefreshing.set(false);
      },
      error: (err) => {
        console.error('Failed to load system observability metrics', err);
        this.isLoading.set(false);
        this.isRefreshing.set(false);
      }
    });
  }

  private startAutoRefresh(): void {
    const sec = this.refreshIntervalSeconds();
    if (sec <= 0) return;

    this.timerHandle = setInterval(() => {
      this.fetchMetrics(false);
    }, sec * 1000);
  }

  private stopAutoRefresh(): void {
    if (this.timerHandle) {
      clearInterval(this.timerHandle);
      this.timerHandle = null;
    }
  }

  private updateCharts(data: ObservabilityMetricsResponse): void {
    if (!data.timeSeries) return;

    // 1. CPU & Memory Line Chart
    if (!this.cpuMemChart && this.cpuMemCanvas) {
      this.initCpuMemChart(data);
    } else if (this.cpuMemChart) {
      this.cpuMemChart.data.labels = data.timeSeries.labels;
      this.cpuMemChart.data.datasets[0].data = data.timeSeries.cpuHistory;
      this.cpuMemChart.data.datasets[1].data = data.timeSeries.memoryHistory;
      this.cpuMemChart.update('none'); // smooth update
    }

    // 2. Throughput Chart
    if (!this.throughputChart && this.throughputCanvas) {
      this.initThroughputChart(data);
    } else if (this.throughputChart) {
      this.throughputChart.data.labels = data.timeSeries.labels;
      this.throughputChart.data.datasets[0].data = data.timeSeries.requestRateHistory;
      this.throughputChart.update('none');
    }
  }

  private initCpuMemChart(data: ObservabilityMetricsResponse): void {
    const ctx = this.cpuMemCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.cpuMemChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: data.timeSeries.labels,
        datasets: [
          {
            label: 'CPU Usage (%)',
            data: data.timeSeries.cpuHistory,
            borderColor: '#3b82f6',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            yAxisID: 'yCpu',
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointHoverRadius: 6
          },
          {
            label: 'RAM Working Set (MB)',
            data: data.timeSeries.memoryHistory,
            borderColor: '#8b5cf6',
            backgroundColor: 'rgba(139, 92, 246, 0.1)',
            yAxisID: 'yMem',
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointHoverRadius: 6
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: { position: 'top', labels: { boxWidth: 12, usePointStyle: true } }
        },
        scales: {
          x: { grid: { display: false } },
          yCpu: {
            type: 'linear',
            display: true,
            position: 'left',
            min: 0,
            max: 100,
            title: { display: true, text: 'CPU %' }
          },
          yMem: {
            type: 'linear',
            display: true,
            position: 'right',
            grid: { drawOnChartArea: false },
            title: { display: true, text: 'Memory (MB)' }
          }
        }
      }
    });
  }

  private initThroughputChart(data: ObservabilityMetricsResponse): void {
    const ctx = this.throughputCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.throughputChart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: data.timeSeries.labels,
        datasets: [
          {
            label: 'Throughput (req/sec)',
            data: data.timeSeries.requestRateHistory,
            backgroundColor: 'rgba(16, 185, 129, 0.7)',
            borderColor: '#10b981',
            borderRadius: 4
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top', labels: { boxWidth: 12 } }
        },
        scales: {
          x: { grid: { display: false } },
          y: { beginAtZero: true, title: { display: true, text: 'Requests / sec' } }
        }
      }
    });
  }
}
