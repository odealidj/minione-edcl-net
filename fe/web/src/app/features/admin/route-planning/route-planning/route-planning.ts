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

  ngOnInit(): void {
    this.loadDrivers();
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
}
