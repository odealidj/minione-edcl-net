import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SyncMonitoringService, SyncSession, IngestionError, IngestionMetricsEvent } from '../../../core/services/sync-monitoring.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-sync-monitoring',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sync-monitoring.html',
  styleUrl: './sync-monitoring.css'
})
export class SyncMonitoring implements OnInit, OnDestroy {
  private syncService = inject(SyncMonitoringService);
  private sseSubscription?: Subscription;

  activeTab = signal<'dashboard' | 'dlq'>('dashboard');

  // Dashboard Data
  currentMetrics = signal<IngestionMetricsEvent | null>(null);
  sessions = signal<SyncSession[]>([]);

  // DLQ Data
  errors = signal<IngestionError[]>([]);
  isRetrying = signal(false);

  ngOnInit(): void {
    this.loadSessions();
    this.loadErrors();
    this.subscribeToMetrics();
  }

  ngOnDestroy(): void {
    this.sseSubscription?.unsubscribe();
  }

  loadSessions() {
    this.syncService.getSessions().subscribe({
      next: (res) => this.sessions.set(res.data),
      error: (err) => console.error('Failed to load sessions', err)
    });
  }

  loadErrors() {
    this.syncService.getErrors().subscribe({
      next: (res) => this.errors.set(res.data),
      error: (err) => console.error('Failed to load errors', err)
    });
  }

  subscribeToMetrics() {
    this.sseSubscription = this.syncService.getMetricsStream().subscribe({
      next: (metrics) => {
        this.currentMetrics.set(metrics);
        // Refresh sessions list if a session completes
        if (metrics.Status === 'COMPLETED' || metrics.Status === 'FAILED') {
          setTimeout(() => this.loadSessions(), 1000);
        }
      }
    });
  }

  setTab(tab: 'dashboard' | 'dlq') {
    this.activeTab.set(tab);
    if (tab === 'dashboard') this.loadSessions();
    if (tab === 'dlq') this.loadErrors();
  }

  retryAllErrors() {
    const errorIds = this.errors().map(e => e.id);
    if (errorIds.length === 0) return;

    if (confirm(`Are you sure you want to retry ${errorIds.length} error(s)?`)) {
      this.isRetrying.set(true);
      this.syncService.retryErrors(errorIds).subscribe({
        next: (res) => {
          alert(`Successfully queued ${res.data.retried} error(s) for retry.`);
          this.loadErrors();
          this.isRetrying.set(false);
        },
        error: (err) => {
          console.error('Failed to retry errors', err);
          alert('Failed to retry errors. See console for details.');
          this.isRetrying.set(false);
        }
      });
    }
  }

  getProgressBarWidth(metrics: IngestionMetricsEvent | null): string {
    if (!metrics || metrics.TotalProcessed === 0) return '0%';
    const progress = (metrics.SuccessCount + metrics.FailedCount) / metrics.TotalProcessed * 100;
    return `${Math.min(progress, 100)}%`;
  }

  parseEventBreakdown(breakdownJson?: string): { event: string, count: number }[] {
    if (!breakdownJson) return [];
    try {
      const parsed = JSON.parse(breakdownJson);
      return Object.keys(parsed).map(key => ({
        event: key,
        count: parsed[key]
      }));
    } catch (e) {
      return [];
    }
  }
}
