import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { RoutePlanningService } from '../../../../core/services/route-planning.service';
import { PickupOrder } from '../../../../core/models/route-planning.model';
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

  orders: PickupOrder[] = [];
  meta: PaginationMeta | null = null;
  search: string = '';
  loading: boolean = false;
  deletingId: number | null = null;
  math = Math;

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(page: number = 1) {
    this.loading = true;
    this.routeService.getPickupOrders(page, 10, this.search).subscribe({
      next: (res) => {
        this.orders = res.data;
        this.meta = res.pagination ?? null;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  onSearch() {
    this.loadOrders(1);
  }

  changePage(page: number) {
    this.loadOrders(page);
  }

  get pages(): number[] {
    if (!this.meta) return [];
    return Array.from({ length: this.meta.total_pages }, (_, i) => i + 1);
  }

  deleteOrder(id: number) {
    if (confirm('Are you sure you want to delete this route plan?')) {
      this.deletingId = id;
      this.routeService.deletePickupOrder(id).subscribe({
        next: () => {
          this.loadOrders(this.meta?.page ?? 1);
          this.deletingId = null;
        },
        error: () => this.deletingId = null
      });
    }
  }
}
