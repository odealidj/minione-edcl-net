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

export interface Transporter {
  id: number;
  name: string;
  createdBy: string;
  createdOnUtc: string;
}

// Master Data Models
export interface Driver {
  id: number;
  name: string;
  nik: string;
  phoneNumber: string;
  transporterId?: number | null;
  transporterName?: string | null;
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
  transporterId?: number | null;
  transporterName?: string | null;
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
