import { Component, inject, OnInit, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { RoutePlanningService } from '../../../../core/services/route-planning.service';
import { MasterDataService } from '../../../../core/services/master-data.service';
import { PickupOrder, BffManifestDetail } from '../../../../core/models/route-planning.model';
import { PaginationMeta } from '../../../../core/models/api.model';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-route-planning',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './route-planning.html'
})
export class RoutePlanningComponent implements OnInit {
  private routeService = inject(RoutePlanningService);
  private masterDataService = inject(MasterDataService);
  private cdr = inject(ChangeDetectorRef);

  orders: PickupOrder[] = [];
  meta: PaginationMeta | null = null;
  search: string = '';
  loading: boolean = false;
  deletingId: number | null = null;
  math = Math;

  expandedRows: Set<number> = new Set();
  orderDetailsCache: Map<number, PickupOrder> = new Map();
  loadingDetails: Set<number> = new Set();

  selectedManifest: BffManifestDetail | null = null;
  loadingManifest: boolean = false;

  driversMap: Map<number, string> = new Map();
  suppliersMap: Map<number, string> = new Map();

  manifestPageSize: number = 30;
  manifestPageMap: Map<string, number> = new Map();

  ngOnInit(): void {
    this.loadDrivers();
    this.loadSuppliers();
    this.loadOrders();
  }

  loadDrivers() {
    this.masterDataService.getDrivers('', 1, 100).subscribe({
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

  pages: number[] = [];

  loadOrders(page: number = 1) {
    this.loading = true;
    this.routeService.getPickupOrders(page, 10, this.search).subscribe({
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

  deleteOrder(id: number) {
    if (confirm('Are you sure you want to delete this route plan?')) {
      this.deletingId = id;
      this.routeService.deletePickupOrder(id).subscribe({
        next: () => {
          this.loadOrders(this.meta?.page ?? 1);
          this.deletingId = null;
          this.expandedRows.delete(id);
          this.orderDetailsCache.delete(id);
        },
        error: () => this.deletingId = null
      });
    }
  }

  toggleRow(orderId: number) {
    if (this.expandedRows.has(orderId)) {
      this.expandedRows.delete(orderId);
      this.cdr.markForCheck();
      return;
    }

    this.expandedRows.add(orderId);
    this.cdr.markForCheck();

    // Fetch details if not in cache
    if (!this.orderDetailsCache.has(orderId)) {
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
  }

  viewManifest(manifestNo: string) {
    this.loadingManifest = true;
    this.selectedManifest = null;
    this.cdr.markForCheck();
    
    // Using a modal element from daisyUI
    const modal = document.getElementById('manifest_modal') as HTMLDialogElement;
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
    const modal = document.getElementById('manifest_modal') as HTMLDialogElement;
    if (modal) modal.close();
    this.selectedManifest = null;
    this.cdr.markForCheck();
  }

  // Level 3 (Manifest) Pagination Methods
  manifestDataMap: Map<string, { items: any[], meta: any, loading: boolean }> = new Map();

  getManifestData(orderId: number, stopIndex: number) {
    const key = `${orderId}_${stopIndex}`;
    return this.manifestDataMap.get(key) || { items: [], meta: null, loading: false };
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
