import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { RoutePlanningService } from '../../../core/services/route-planning.service';
import { MasterDataService } from '../../../core/services/master-data.service';
import { PickupOrder, BffManifestDetail, BffManifestPart } from '../../../core/models/route-planning.model';
import { PaginationMeta } from '../../../core/models/api.model';

@Component({
  selector: 'app-delivery-monitoring',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './delivery-monitoring.html'
})
export class DeliveryMonitoringComponent implements OnInit {
  private routeService = inject(RoutePlanningService);
  private masterDataService = inject(MasterDataService);
  private cdr = inject(ChangeDetectorRef);

  orders: PickupOrder[] = [];
  meta: PaginationMeta | null = null;
  search: string = '';
  loading: boolean = false;
  math = Math;

  expandedRows: Set<number> = new Set();
  orderDetailsCache: Map<number, PickupOrder> = new Map();
  loadingDetails: Set<number> = new Set();

  selectedManifest: BffManifestDetail | null = null;
  loadingManifest: boolean = false;
  
  // Filter state
  filterState = {
    poNo: '',
    manifestNo: '',
    pickupDate: '',
    routeCode: '',
    driverId: '' as string | number
  };
  
  driversMap: Map<number, string> = new Map();
  suppliersMap: Map<number, string> = new Map();
  routesMap: Map<string, string> = new Map();

  manifestPageSize: number = 30;

  // Manifest Parts Pagination & Filtering
  partSearch: string = '';
  partPage: number = 1;
  partPageSize: number = 20;

  // Manifest Kanban Pagination & Filtering
  kanbanSearch: string = '';
  kanbanPage: number = 1;
  kanbanPageSize: number = 20;

  // Modal active tab
  activeTab: 'parts' | 'kanbans' = 'parts';

  get filteredParts(): BffManifestPart[] {
    if (!this.selectedManifest || !this.selectedManifest.parts) return [];
    if (!this.partSearch) return this.selectedManifest.parts;
    
    const searchLower = this.partSearch.toLowerCase();
    return this.selectedManifest.parts.filter(p => 
      p.partNo.toLowerCase().includes(searchLower) || 
      p.uniqNo.toLowerCase().includes(searchLower)
    );
  }

  get paginatedParts(): BffManifestPart[] {
    const parts = this.filteredParts;
    const start = (this.partPage - 1) * this.partPageSize;
    return parts.slice(start, start + this.partPageSize);
  }

  get partTotalPages(): number {
    return Math.ceil(this.filteredParts.length / this.partPageSize) || 1;
  }

  get partPages(): number[] {
    return Array.from({ length: this.partTotalPages }, (_, i) => i + 1);
  }

  onPartSearch() {
    this.partPage = 1;
    this.cdr.markForCheck();
  }

  applyFilter() {
    this.loadOrders(1);
  }

  resetFilter() {
    this.filterState = {
      poNo: '',
      manifestNo: '',
      pickupDate: '',
      routeCode: '',
      driverId: ''
    };
    this.loadOrders(1);
  }

  changePartPage(page: number) {
    if (page >= 1 && page <= this.partTotalPages) {
      this.partPage = page;
      this.cdr.markForCheck();
    }
  }

  get filteredKanbans(): any[] {
    if (!this.selectedManifest || !this.selectedManifest.kanbans) return [];
    if (!this.kanbanSearch) return this.selectedManifest.kanbans;
    
    const searchLower = this.kanbanSearch.toLowerCase();
    return this.selectedManifest.kanbans.filter((k: any) => 
      k.partNo.toLowerCase().includes(searchLower) || 
      k.kanbanCd.toLowerCase().includes(searchLower)
    );
  }

  get paginatedKanbans(): any[] {
    const kanbans = this.filteredKanbans;
    const start = (this.kanbanPage - 1) * this.kanbanPageSize;
    return kanbans.slice(start, start + this.kanbanPageSize);
  }

  get kanbanTotalPages(): number {
    return Math.ceil(this.filteredKanbans.length / this.kanbanPageSize) || 1;
  }

  get kanbanPages(): number[] {
    return Array.from({ length: this.kanbanTotalPages }, (_, i) => i + 1);
  }

  onKanbanSearch() {
    this.kanbanPage = 1;
    this.cdr.markForCheck();
  }

  changeKanbanPage(page: number) {
    if (page >= 1 && page <= this.kanbanTotalPages) {
      this.kanbanPage = page;
      this.cdr.markForCheck();
    }
  }

  ngOnInit(): void {
    this.loadDrivers();
    this.loadSuppliers();
    this.loadRoutes();
    this.loadOrders(1);
  }

  loadDrivers() {
    this.masterDataService.getDrivers('', 1, 1000).subscribe({
      next: (res) => {
        if (res.data) {
          res.data.forEach(d => this.driversMap.set(d.id, d.name));
          this.cdr.markForCheck();
        }
      }
    });
  }

  loadSuppliers() {
    this.masterDataService.getSuppliers('', 1, 1000).subscribe({
      next: (res) => {
        if (res.data) {
          res.data.forEach(s => this.suppliersMap.set(s.id, `${s.supplierCode} - ${s.name}`));
          this.cdr.markForCheck();
        }
      }
    });
  }

  loadRoutes() {
    this.masterDataService.getRoutes('', 1, 1000).subscribe({
      next: (res) => {
        if (res.data) {
          res.data.forEach(r => this.routesMap.set(r.routeCode, r.routeCode));
          this.cdr.markForCheck();
        }
      }
    });
  }

  pages: number[] = [];

  loadOrders(page: number = 1) {
    this.loading = true;
    this.cdr.markForCheck();
    this.routeService.getPickupOrders(page, 10, this.filterState).subscribe({
      next: (res) => {
        this.orders = res.data ?? [];
        this.meta = res.pagination ?? null;
        if (this.meta) {
          this.pages = Array.from({ length: this.meta.total_pages }, (_, i) => i + 1);
        } else {
          this.pages = [];
        }
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onSearch() {
    this.loadOrders(1);
  }

  changePage(page: number) {
    this.loadOrders(page);
  }
  
  refresh() {
    this.loadOrders(this.meta?.page ?? 1);
  }

  toggleRow(orderId: number) {
    if (this.expandedRows.has(orderId)) {
      this.expandedRows.delete(orderId);
      this.cdr.markForCheck();
      return;
    }

    this.expandedRows.add(orderId);
    this.cdr.markForCheck();

    // Fetch details if not in cache (or we could always refresh to get latest monitoring data)
    this.loadingDetails.add(orderId);
    this.cdr.markForCheck();
    this.routeService.getPickupOrderById(orderId).subscribe({
      next: (res) => {
        if (res.data) {
          this.orderDetailsCache.set(orderId, res.data);
          // Load page 1 of manifests for each stop
          res.data.details?.forEach((stop, idx) => {
            this.loadStopManifests(orderId, stop.id, idx, 1);
          });
        }
        this.loadingDetails.delete(orderId);
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to load order details', err);
        this.loadingDetails.delete(orderId);
        this.expandedRows.delete(orderId);
        this.cdr.markForCheck();
      }
    });
  }

  viewManifest(manifestNo: string) {
    this.loadingManifest = true;
    this.selectedManifest = null;
    this.partSearch = '';
    this.partPage = 1;
    this.kanbanSearch = '';
    this.kanbanPage = 1;
    this.activeTab = 'parts';
    this.cdr.markForCheck();
    
    const modal = document.getElementById('manifest_modal_monitoring') as HTMLDialogElement;
    if (modal) modal.showModal();

    this.routeService.getManifestDetail(manifestNo).subscribe({
      next: (res) => {
        this.selectedManifest = res.data ?? null;
        this.loadingManifest = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to load manifest details', err);
        this.loadingManifest = false;
        this.cdr.markForCheck();
      }
    });
  }

  closeManifestModal() {
    const modal = document.getElementById('manifest_modal_monitoring') as HTMLDialogElement;
    if (modal) modal.close();
    this.selectedManifest = null;
    this.partSearch = '';
    this.partPage = 1;
    this.kanbanSearch = '';
    this.kanbanPage = 1;
    this.cdr.markForCheck();
  }

  manifestDataMap: Map<string, { items: any[], meta: any, loading: boolean }> = new Map();

  getManifestData(orderId: number, stopIndex: number) {
    const key = `${orderId}_${stopIndex}`;
    return this.manifestDataMap.get(key) || { items: [], meta: null, loading: false };
  }

  changeManifestPage(orderId: number, stopIndex: number, newPage: number) {
    const stopDetails = this.orderDetailsCache.get(orderId)?.details?.[stopIndex];
    if (stopDetails) {
      this.loadStopManifests(orderId, stopDetails.id, stopIndex, newPage);
    }
  }

  loadStopManifests(orderId: number, stopId: number, stopIndex: number, page: number) {
    const key = `${orderId}_${stopIndex}`;
    const currentData = this.manifestDataMap.get(key) || { items: [], meta: null, loading: false };
    this.manifestDataMap.set(key, { ...currentData, loading: true });
    this.cdr.markForCheck();

    this.routeService.getPickupOrderStopManifests(orderId, stopId, page, this.manifestPageSize).subscribe({
      next: (res) => {
        this.manifestDataMap.set(key, {
          items: res.data ?? [],
          meta: res.pagination ?? null,
          loading: false
        });
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to load stop manifests', err);
        this.manifestDataMap.set(key, { ...currentData, loading: false });
        this.cdr.markForCheck();
      }
    });
  }
}
