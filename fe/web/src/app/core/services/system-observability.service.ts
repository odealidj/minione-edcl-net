import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { ObservabilityMetricsResponse } from '../models/system-observability.model';

@Injectable({
  providedIn: 'root'
})
export class SystemObservabilityService {
  private apiUrl = `${environment.apiUrl}/admin/observability`;

  constructor(private http: HttpClient) {}

  getMetrics(): Observable<ApiResponse<ObservabilityMetricsResponse>> {
    return this.http.get<ApiResponse<ObservabilityMetricsResponse>>(`${this.apiUrl}/metrics`);
  }
}
