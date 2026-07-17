import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from './dashboard.service'; // We can reuse ApiResponse or define it locally

export interface BackgroundJobDto {
  jobId: string;
  state: string;
  methodName: string;
  enqueueAt: string | null;
  failedAt: string | null;
  exceptionMessage: string | null;
  exceptionDetails: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class BackgroundJobsService {
  private apiUrl = `${environment.apiUrl}/admin/system-tasks`;

  constructor(private http: HttpClient) {}

  getScheduledJobs(from: number = 0, count: number = 100): Observable<ApiResponse<BackgroundJobDto[]>> {
    return this.http.get<ApiResponse<BackgroundJobDto[]>>(`${this.apiUrl}/scheduled?from=${from}&count=${count}`);
  }

  getFailedJobs(from: number = 0, count: number = 100): Observable<ApiResponse<BackgroundJobDto[]>> {
    return this.http.get<ApiResponse<BackgroundJobDto[]>>(`${this.apiUrl}/failed?from=${from}&count=${count}`);
  }

  requeueJob(jobId: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.apiUrl}/requeue/${jobId}`, {});
  }
}
