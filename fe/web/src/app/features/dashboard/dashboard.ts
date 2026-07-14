import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { DashboardService, DashboardSummaryResponse, LateDepartureAlertDto } from '../../core/services/dashboard.service';
import { FormsModule } from '@angular/forms';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit, AfterViewInit, OnDestroy {
  loading = true;
  summary: DashboardSummaryResponse | null = null;
  selectedDate: string = new Date().toISOString().split('T')[0];

  @ViewChild('routeChart') routeChartRef!: ElementRef<HTMLCanvasElement>;
  private chartInstance: Chart | null = null;

  constructor(private dashboardService: DashboardService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngAfterViewInit(): void {
    // Chart will be initialized after data is loaded
  }

  ngOnDestroy(): void {
    if (this.chartInstance) {
      this.chartInstance.destroy();
    }
  }

  loadData() {
    this.loading = true;
    this.dashboardService.getSummary(this.selectedDate).subscribe({
      next: (res) => {
        console.log('[Dashboard] API response:', res);
        if (res.status === 'success' && res.data) {
          this.summary = res.data;
          this.loading = false;
          this.cdr.detectChanges();
          console.log('[Dashboard] Summary set:', this.summary);
          setTimeout(() => this.renderChart(), 50);
        } else {
          console.warn('[Dashboard] Unexpected response structure:', res);
          this.loading = false;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error('[Dashboard] Failed to load dashboard summary', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onDateChange() {
    this.loadData();
  }

  renderChart() {
    if (!this.summary || !this.routeChartRef) return;

    if (this.chartInstance) {
      this.chartInstance.destroy();
    }

    const labels = this.summary.routeDistributions.map(x => x.routeCode);
    const data = this.summary.routeDistributions.map(x => x.orderCount);

    const ctx = this.routeChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chartInstance = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: labels,
        datasets: [{
          data: data,
          backgroundColor: [
            '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6'
          ],
          borderWidth: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'right',
            labels: {
              boxWidth: 12,
              font: { size: 11 }
            }
          }
        },
        cutout: '70%'
      }
    });
  }
}
