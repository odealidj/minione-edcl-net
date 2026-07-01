export interface PickupOrderKanban {
  id: number;
  kanbanCode: string;
  status: string;
  scannedAt: string;
}

export interface PickupOrderManifest {
  id: number;
  manifestNo: string;
  status: string;
  totalKanban: number;
  scannedKanban: number;
  orderType: string;
  totalSkid: number;
  dockCode: string;
  totalPart?: number;
  kanbans: PickupOrderKanban[];
}

export interface PickupOrderDetail {
  id: number;
  supplierId: number;
  sequence: number;
  status: string;
  arrivedAt?: string;
  pickedUpAt?: string;
  manifests: PickupOrderManifest[];
}

export interface PickupOrder {
  id: number;
  driverId?: number;
  truckId?: number;
  poNo: string;
  pickupDate: string;
  routeCode: string;
  cycleCode: string;
  estimatedDepartureTime: string;
  status: string;
  startedAt?: string;
  completedAt?: string;
  details?: PickupOrderDetail[];
}

export interface CreatePickupOrderManifestDto {
  manifestNo: string;
  totalKanban: number;
  orderType: string;
  totalSkid: number;
  dockCode: string;
  totalPart?: number;
}

export interface CreatePickupOrderDetailDto {
  supplierId: number;
  sequence: number;
  manifests: CreatePickupOrderManifestDto[];
}

export interface CreatePickupOrderCommand {
  driverId?: number;
  truckId?: number;
  poNo: string;
  pickupDate: string;
  routeCode: string;
  cycleCode: string;
  estimatedDepartureTime: string;
  stops: CreatePickupOrderDetailDto[];
}

export interface UpdatePickupOrderManifestDto {
  id: number;
  manifestNo: string;
  totalKanban: number;
  orderType: string;
  totalSkid: number;
  dockCode: string;
  totalPart?: number;
}

export interface UpdatePickupOrderDetailDto {
  id: number;
  supplierId: number;
  sequence: number;
  manifests: UpdatePickupOrderManifestDto[];
}

export interface UpdatePickupOrderCommand {
  id: number;
  driverId?: number;
  truckId?: number;
  poNo: string;
  pickupDate: string;
  routeCode: string;
  cycleCode: string;
  estimatedDepartureTime: string;
  stops: UpdatePickupOrderDetailDto[];
}

export interface BffManifestPart {
  no: number;
  partNo: string;
  uniqNo: string;
  pcsKbn: number;
  boxType: string;
  noOfKbn: string;
}

export interface BffManifestDetail {
  manifestNo: string;
  orderType: string;
  dockCode: string;
  scanStatus: string;
  totalKanban: number;
  scannedKanban: number;
  orderNo: string | null;
  pLaneNo: string | null;
  parts: BffManifestPart[];
}
