import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SyncSession {
  id: number;
  sessionDate: string;
  startTime: string;
  endTime?: string;
  totalProcessed: number;
  successCount: number;
  failedCount: number;
  eventBreakdown?: string;
  status: string;
}

export interface IngestionError {
  id: number;
  eventType: string;
  payload: string;
  errorMessage: string;
  stackTrace: string;
}

export interface IngestionMetricsEvent {
  SessionId: number;
  SessionDate: string;
  StartTime: string;
  EndTime?: string;
  TotalProcessed: number;
  SuccessCount: number;
  FailedCount: number;
  EventBreakdown?: string;
  Status: string;
}

@Injectable({
  providedIn: 'root'
})
export class SyncMonitoringService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/cargo/ingestion`;

  getSessions(): Observable<{ data: SyncSession[] }> {
    return this.http.get<{ data: SyncSession[] }>(`${this.apiUrl}/sessions`);
  }

  getErrors(): Observable<{ data: IngestionError[] }> {
    return this.http.get<{ data: IngestionError[] }>(`${this.apiUrl}/errors`);
  }

  retryErrors(errorIds: number[]): Observable<{ data: any }> {
    return this.http.post<{ data: any }>(`${this.apiUrl}/errors/retry`, { errorIds });
  }

  getMetricsStream(): Observable<IngestionMetricsEvent> {
    return new Observable(observer => {
      const eventSource = new EventSource(`${this.apiUrl}/metrics/stream`);
      
      eventSource.onmessage = event => {
        try {
          const data = JSON.parse(event.data);
          observer.next(data);
        } catch (err) {
          console.error('Error parsing SSE data', err);
        }
      };

      eventSource.onerror = error => {
        // Optional: you can complete or error out, but SSE auto-reconnects.
        console.error('SSE Error', error);
      };

      return () => {
        eventSource.close();
      };
    });
  }
}
