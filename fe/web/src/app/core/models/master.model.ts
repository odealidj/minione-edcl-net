// Admin Models
export interface User {
  id: number;
  username?: string;
  email: string;
  name: string;
  roleCode: string;
  status?: string;
  createdBy?: string;
  createdOnUtc?: string;
}

export interface LogisticPartner {
  id: number;
  code: string;
  name: string;
  createdBy: string;
  createdOnUtc: string;
}

export interface Route {
  id: number;
  routeCode: string;
  cycleCode: string;
}

// Master Data Models
export interface Driver {
  id: number;
  name: string;
  nik: string;
  phoneNumber: string;
  logisticPartnerId?: number | null;
  logisticPartnerName?: string | null;
  createdBy: string;
  createdOnUtc: string;
}

export interface Supplier {
  id: number;
  supplierCode: string;
  name: string;
  address: string;
  latitude?: number | null;
  longitude?: number | null;
  geofenceRadiusMeters?: number | null;
  createdBy: string;
  createdOnUtc: string;
}

export interface Truck {
  id: number;
  plateNumber: string;
  vehicleType: string;
  logisticPartnerId?: number | null;
  logisticPartnerName?: string | null;
  isActive?: boolean;
  createdBy: string;
  createdOnUtc: string;
}

// Cargo Models
export interface Manifest {
  id: number;
  manifestNo: string;
  supplierCode: string;
  supplierName: string;
  status: string;
  totalKanbans: number;
  totalParts: number;
}

export interface ManifestKanban {
  id: number;
  partNo: string;
  partName: string;
  kanbanCd: string;
}

export interface PendingManifestSupplier {
  supplierCode: string;
  supplierName: string;
}

export interface ManifestProblem {
  id: number;
  operationType: string;
  payload: string;
  description: string;
  status: string;
  occurredAt: string;
  manifestNo?: string | null;
}
