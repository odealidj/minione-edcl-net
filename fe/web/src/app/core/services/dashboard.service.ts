import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DashboardKpiDto {
  totalOrders: number;
  pendingOrders: number;
  onProgressOrders: number;
  completedOrders: number;
  totalKanban: number;
  scheduledJobsCount: number;
  failedJobsCount: number;
}

export interface RouteDistributionDto {
  routeCode: string;
  orderCount: number;
}

export interface LateDepartureAlertDto {
  poNo: string;
  routeCode: string;
  estimatedDepartureTime: string;
  status: string;
}

export interface DashboardSummaryResponse {
  kpis: DashboardKpiDto;
  routeDistributions: RouteDistributionDto[];
  lateDepartures: LateDepartureAlertDto[];
}

export interface ApiResponse<T> {
  data: T;
  status: string;
  code: number;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/admin/dashboard`;

  constructor(private http: HttpClient) {}

  getSummary(date?: string): Observable<ApiResponse<DashboardSummaryResponse>> {
    let params = {};
    if (date) {
      params = { date };
    }
    return this.http.get<ApiResponse<DashboardSummaryResponse>>(`${this.apiUrl}/summary`, { params });
  }
}
