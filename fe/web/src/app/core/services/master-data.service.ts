import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { Driver, Supplier, Truck } from '../models/master.model';

@Injectable({
  providedIn: 'root'
})
export class MasterDataService {
  private http = inject(HttpClient);

  // Drivers
  getDrivers(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Driver[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<Driver[]>>(`${environment.apiUrl}/auth/drivers`, { params });
  }

  createDriver(name: string, nik: string, phoneNumber: string, transporterId?: number | null): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/auth/drivers`, { name, nik, phoneNumber, transporterId });
  }

  updateDriver(id: number, name: string, nik: string, phoneNumber: string, transporterId?: number | null): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/auth/drivers/${id}`, { name, nik, phoneNumber, transporterId });
  }

  deleteDriver(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/auth/drivers/${id}`);
  }

  // Suppliers
  getSuppliers(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Supplier[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<Supplier[]>>(`${environment.apiUrl}/master/suppliers`, { params });
  }

  createSupplier(supplierCode: string, name: string, address: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/suppliers`, { supplierCode, name, address });
  }

  updateSupplier(id: number, supplierCode: string, name: string, address: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/suppliers/${id}`, { supplierCode, name, address });
  }

  deleteSupplier(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/master/suppliers/${id}`);
  }

  // Trucks
  getTrucks(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Truck[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<Truck[]>>(`${environment.apiUrl}/master/trucks`, { params });
  }

  createTruck(plateNumber: string, truckType: string, truckCapacity: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/trucks`, { plateNumber, truckType, truckCapacity });
  }

  updateTruck(id: number, plateNumber: string, truckType: string, truckCapacity: number): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/trucks/${id}`, { plateNumber, truckType, truckCapacity });
  }

  deleteTruck(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/master/trucks/${id}`);
  }
}
