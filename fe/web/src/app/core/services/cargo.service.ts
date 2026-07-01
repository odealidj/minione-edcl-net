import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { Manifest, ManifestKanban } from '../models/master.model';

@Injectable({
  providedIn: 'root'
})
export class CargoService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/master/cargo';

  getManifests(search?: string, supplierCode?: string, status?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Manifest[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (supplierCode) params = params.set('supplierCode', supplierCode);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<Manifest[]>>(`${this.baseUrl}/manifests`, { params });
  }

  getManifestKanbans(manifestId: number, search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<ManifestKanban[]>> {
    let params = new HttpParams().set('manifestId', manifestId).set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<ManifestKanban[]>>(`${this.baseUrl}/manifest-kanbans`, { params });
  }
}
