import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { User, LogisticPartner } from '../models/master.model';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  
  // Users
  getUsers(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<User[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<User[]>>(`${environment.apiUrl}/auth/users`, { params });
  }

  updateUserRole(id: number, role: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/auth/users/${id}/role`, { userId: id, roleCode: role });
  }

  // LogisticPartners
  getLogisticPartners(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<LogisticPartner[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<LogisticPartner[]>>(`${environment.apiUrl}/master/logistic-partners`, { params });
  }

  createLogisticPartner(code: string, name: string, gpsVendorIds: number[] = []): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/logistic-partners`, { code, name, gpsVendorIds });
  }

  updateLogisticPartner(id: number, code: string, name: string, gpsVendorIds: number[] = []): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/logistic-partners/${id}`, { id, code, name, gpsVendorIds });
  }

  deleteLogisticPartner(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/master/logistic-partners/${id}`);
  }

  // Routes
  getRoutes(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<any>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<any>>(`${environment.apiUrl}/master/routes`, { params });
  }

  createRoute(routeCode: string, cycleCode: string, logisticPartnerId: number | null): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/routes`, { routeCode, cycleCode, logisticPartnerId });
  }

  updateRoute(id: number, routeCode: string, cycleCode: string, logisticPartnerId: number | null): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/routes/${id}`, { id, routeCode, cycleCode, logisticPartnerId });
  }

  deleteRoute(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/master/routes/${id}`);
  }
}
