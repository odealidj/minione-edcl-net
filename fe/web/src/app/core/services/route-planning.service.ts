import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { PickupOrder, CreatePickupOrderCommand, UpdatePickupOrderCommand } from '../models/route-planning.model';

@Injectable({
  providedIn: 'root'
})
export class RoutePlanningService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/pickup-orders`;

  getPickupOrders(page: number = 1, pageSize: number = 10, search?: string): Observable<ApiResponse<PickupOrder[]>> {
    let params = new HttpParams()
      .set('pageNumber', page)
      .set('pageSize', pageSize);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<ApiResponse<PickupOrder[]>>(this.apiUrl, { params });
  }

  getPickupOrderById(id: number): Observable<ApiResponse<PickupOrder>> {
    return this.http.get<ApiResponse<PickupOrder>>(`${this.apiUrl}/${id}`);
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
}
