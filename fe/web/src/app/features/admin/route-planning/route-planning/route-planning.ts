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
    this.routeService.getPickupOrders(
      page,
      10,
      this.search
    ).subscribe({
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
        },
        error: () => {
          this.deletingId = null;
          this.cdr.markForCheck();
        }
      });
    }
  }
}
