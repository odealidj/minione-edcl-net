import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { User, Transporter } from '../models/master.model';

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

  // Transporters
  getTransporters(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Transporter[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<Transporter[]>>(`${environment.apiUrl}/master/transporters`, { params });
  }

  createTransporter(name: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/transporters`, { name });
  }

  updateTransporter(id: number, name: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/transporters/${id}`, { id, name });
  }

  deleteTransporter(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/master/transporters/${id}`);
  }
}
