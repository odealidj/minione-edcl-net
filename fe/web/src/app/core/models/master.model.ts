// Admin Models
export interface User {
  id: string;
  username: string;
  email: string;
  name: string;
  role: string;
  status: string;
  createdBy: string;
  createdOnUtc: string;
}

export interface Transporter {
  id: string; // GUID
  name: string;
  createdBy: string;
  createdOnUtc: string;
}

// Master Data Models
export interface Driver {
  id: string; // GUID
  name: string;
  nik: string;
  phoneNumber: string;
  createdBy: string;
  createdOnUtc: string;
}

export interface Supplier {
  id: string; // GUID
  supplierCode: string;
  name: string;
  address: string;
  createdBy: string;
  createdOnUtc: string;
}

export interface Truck {
  id: string; // GUID
  plateNumber: string;
  truckType: string;
  truckCapacity: number;
  createdBy: string;
  createdOnUtc: string;
}

// Cargo Models
export interface Manifest {
  id: number;
  manifestNo: string;
  supplierName: string;
  totalKanbans: number;
  totalParts: number;
}

export interface ManifestKanban {
  id: number;
  partNo: string;
  partName: string;
  kanbanCd: string;
}
