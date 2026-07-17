import { Component, OnInit } from '@angular/core';
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

  constructor(private jobsService: BackgroundJobsService) {}

  ngOnInit(): void {
    this.loadData();
  }

  setTab(tab: 'scheduled' | 'failed') {
    this.activeTab = tab;
    this.loadData();
  }

  loadData() {
    this.loading = true;
    if (this.activeTab === 'scheduled') {
      this.jobsService.getScheduledJobs().subscribe({
        next: (res) => {
          this.scheduledJobs = res.data || [];
          this.loading = false;
        },
        error: () => this.loading = false
      });
    } else {
      this.jobsService.getFailedJobs().subscribe({
        next: (res) => {
          this.failedJobs = res.data || [];
          this.loading = false;
        },
        error: () => this.loading = false
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
