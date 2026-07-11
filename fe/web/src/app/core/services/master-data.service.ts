import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';
import { Driver, Supplier, Truck, Transporter } from '../models/master.model';

@Injectable({
  providedIn: 'root'
})
export class MasterDataService {
  private http = inject(HttpClient);

  // Transporters
  getTransporters(search?: string, page: number = 1, pageSize: number = 100): Observable<ApiResponse<Transporter[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<Transporter[]>>(`${environment.apiUrl}/master/transporters`, { params });
  }

  // Drivers
  getDrivers(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Driver[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<Driver[]>>(`${environment.apiUrl}/master/drivers`, { params });
  }

  createDriver(name: string, nik: string, phoneNumber: string, transporterId?: number | null): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/drivers`, { name, nik, phoneNumber, transporterId });
  }

  updateDriver(id: number, name: string, nik: string, phoneNumber: string, transporterId?: number | null): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/drivers/${id}`, { id, name, nik, phoneNumber, transporterId });
  }

  deleteDriver(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/master/drivers/${id}`);
  }

  // Suppliers
  getSuppliers(search?: string, page: number = 1, pageSize: number = 10): Observable<ApiResponse<Supplier[]>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<Supplier[]>>(`${environment.apiUrl}/master/suppliers`, { params });
  }

  createSupplier(supplierCode: string, name: string, address: string, latitude?: number | null, longitude?: number | null, geofenceRadiusMeters?: number | null): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/suppliers`, { supplierCode, name, address, latitude, longitude, geofenceRadiusMeters });
  }

  updateSupplier(id: number, supplierCode: string, name: string, address: string, latitude?: number | null, longitude?: number | null, geofenceRadiusMeters?: number | null): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/suppliers/${id}`, { id, supplierCode, name, address, latitude, longitude, geofenceRadiusMeters });
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

  createTruck(plateNumber: string, vehicleType: string, transporterId: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${environment.apiUrl}/master/trucks`, { plateNumber, vehicleType, transporterId });
  }

  updateTruck(id: number, plateNumber: string, vehicleType: string | null, transporterId: number): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/master/trucks/${id}`, { id, plateNumber, vehicleType, transporterId });
  }

  deleteTruck(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/master/trucks/${id}`);
  }

  assignDriverToTruck(truckId: number, driverId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/master/trucks/${truckId}/assign`, { driverId });
  }

  unassignDriverFromTruck(truckId: number, driverId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/master/trucks/${truckId}/unassign`, { driverId });
  }

  getAvailableTrucks(transporterId: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${environment.apiUrl}/master/trucks/assignments/available-trucks?transporterId=${transporterId}`);
  }

  getAvailableDrivers(transporterId: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${environment.apiUrl}/master/trucks/assignments/available-drivers?transporterId=${transporterId}`);
  }

  getTruckAssignments(): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${environment.apiUrl}/master/trucks/assignments`);
  }
}
