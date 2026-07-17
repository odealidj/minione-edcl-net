import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { BackgroundJobsService, BackgroundJobDto } from '../../../core/services/background-jobs.service';

@Component({
  selector: 'app-background-jobs',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './background-jobs.html',
})
export class BackgroundJobsComponent implements OnInit {
  activeTab: 'scheduled' | 'failed' = 'scheduled';
  
  scheduledJobs: BackgroundJobDto[] = [];
  failedJobs: BackgroundJobDto[] = [];
  
  loading: boolean = false;
  selectedError: string | null = null;

  constructor(
    private jobsService: BackgroundJobsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  setTab(tab: 'scheduled' | 'failed') {
    this.activeTab = tab;
    this.loadData();
  }

  loadData() {
    this.loading = true;
    this.cdr.detectChanges();
    if (this.activeTab === 'scheduled') {
      this.jobsService.getScheduledJobs().subscribe({
        next: (res) => {
          this.scheduledJobs = res?.data || [];
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("Failed to load scheduled jobs:", err);
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
    } else {
      this.jobsService.getFailedJobs().subscribe({
        next: (res) => {
          this.failedJobs = res?.data || [];
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error("Failed to load failed jobs:", err);
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
    }
  }

  retryJob(jobId: string) {
    this.jobsService.requeueJob(jobId).subscribe({
      next: () => {
        // Reload failed jobs after requeue
        this.loadData();
      }
    });
  }

  showError(errorDetails: string | null) {
    this.selectedError = errorDetails;
  }

  closeError() {
    this.selectedError = null;
  }
}
