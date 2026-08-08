import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { PickupOrder, CreatePickupOrderCommand, UpdatePickupOrderCommand, BffManifestDetail } from '../models/route-planning.model';

@Injectable({
  providedIn: 'root'
})
export class RoutePlanningService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/pickup-orders`;

  getPickupOrders(
    page: number = 1,
    limit: number = 10,
    filters: any = {}
  ): Observable<ApiResponse<PickupOrder[]>> {
    let params = new HttpParams()
      .set('PageNumber', page.toString())
      .set('PageSize', limit.toString());

    if (filters.poNo) params = params.set('PoNo', filters.poNo);
    if (filters.manifestNo) params = params.set('ManifestNo', filters.manifestNo);
    if (filters.pickupDate) params = params.set('PickupDate', filters.pickupDate);
    if (filters.routeCode) params = params.set('RouteCode', filters.routeCode);
    if (filters.driverId) params = params.set('DriverId', filters.driverId.toString());
    if (filters.status) params = params.set('Status', filters.status);

    return this.http.get<ApiResponse<PickupOrder[]>>(this.apiUrl, { params });
  }

  getPickupOrderById(id: number): Observable<ApiResponse<PickupOrder>> {
    return this.http.get<ApiResponse<PickupOrder>>(`${this.apiUrl}/${id}`);
  }

  getPickupOrderStopManifests(id: number, stopId: number, page: number = 1, pageSize: number = 30): Observable<ApiResponse<any[]>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<ApiResponse<any[]>>(`${this.apiUrl}/${id}/stops/${stopId}/manifests`, { params });
  }

  createPickupOrder(command: CreatePickupOrderCommand): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.apiUrl, command);
  }

  updatePickupOrder(id: number, command: UpdatePickupOrderCommand): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/${id}`, command);
  }

  deletePickupOrder(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/${id}`);
  }

  getManifestDetail(manifestNo: string): Observable<ApiResponse<BffManifestDetail>> {
    const bffUrl = `${environment.apiUrl}/bff/manifests/${encodeURIComponent(manifestNo)}/detail`;
    return this.http.get<ApiResponse<BffManifestDetail>>(bffUrl);
  }

  forceCompletePickupOrder(id: number, reason: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.apiUrl}/${id}/complete`, { reason });
  }
}
