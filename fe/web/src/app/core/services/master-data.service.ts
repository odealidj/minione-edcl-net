import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';

export interface Driver {
  no: number;
  sapId: string;
  name: string;
  nik: string;
  lp: string;
  badge: string;
  isAvailable: boolean;
  createdBy: string;
  creationDate: string;
}

export interface Supplier {
  no: number;
  sapId: string;
  abbreviation: string;
  name: string;
  createdBy: string;
  creationDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class MasterDataService {

  constructor() { }

  getDrivers(): Observable<Driver[]> {
    const mockDrivers: Driver[] = [
      { no: 1, sapId: 'D001', name: 'BUDI SANTOSO', nik: '3201111222333444', lp: 'LP-JABAR', badge: 'Active', isAvailable: true, createdBy: 'Admin', creationDate: '2023-01-01' },
      { no: 2, sapId: 'D002', name: 'AGUS SUPRIYADI', nik: '3201111222333555', lp: 'LP-JATENG', badge: 'Active', isAvailable: false, createdBy: 'Admin', creationDate: '2023-02-15' },
      { no: 3, sapId: 'D003', name: 'JOKO WIDODO', nik: '3201111222333666', lp: 'LP-JATIM', badge: 'Inactive', isAvailable: false, createdBy: 'Admin', creationDate: '2023-03-20' },
    ];
    return of(mockDrivers).pipe(delay(800)); // Simulate network delay
  }

  getSuppliers(): Observable<Supplier[]> {
    const mockSuppliers: Supplier[] = [
      { no: 1, sapId: 'S001', abbreviation: 'PT. MAKMUR', name: 'PT. MAKMUR SENTOSA', createdBy: 'Admin', creationDate: '2023-01-05' },
      { no: 2, sapId: 'S002', abbreviation: 'CV. JAYA', name: 'CV. JAYA ABADI', createdBy: 'Admin', creationDate: '2023-01-10' },
    ];
    return of(mockSuppliers).pipe(delay(800)); // Simulate network delay
  }
}
