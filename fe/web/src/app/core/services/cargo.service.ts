import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { Manifest, ManifestKanban, PendingManifestSupplier, ManifestProblem } from '../models/master.model';

@Injectable({
  providedIn: 'root'
})
export class CargoService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/master/cargo';

  getManifests(search?: string, supplierCode?: string, status?: string, isAssignedToRoute?: boolean, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Manifest[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (supplierCode) params = params.set('supplierCode', supplierCode);
    if (status) params = params.set('status', status);
    if (isAssignedToRoute !== undefined) params = params.set('isAssignedToRoute', isAssignedToRoute);
    return this.http.get<ApiResponse<Manifest[]>>(`${this.baseUrl}/manifests`, { params });
  }

  getManifestKanbans(manifestId: number, search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<ManifestKanban[]>> {
    let params = new HttpParams().set('manifestId', manifestId).set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<ManifestKanban[]>>(`${this.baseUrl}/manifest-kanbans`, { params });
  }

  getPendingManifestSuppliers(): Observable<ApiResponse<PendingManifestSupplier[]>> {
    return this.http.get<ApiResponse<PendingManifestSupplier[]>>(`${this.baseUrl}/manifests/pending-suppliers`);
  }

  getManifestProblems(search?: string, status?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<ManifestProblem[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<ManifestProblem[]>>(`${this.baseUrl}/manifest-problems`, { params });
  }

  resolveManifestProblem(id: number, reason: string): Observable<ApiResponse<boolean>> {
    return this.http.patch<ApiResponse<boolean>>(`${this.baseUrl}/manifest-problems/${id}/resolve`, { reason });
  }
}
