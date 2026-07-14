import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CargoService } from '../../../core/services/cargo.service';
import { RoutePlanningService } from '../../../core/services/route-planning.service';
import { ManifestProblem } from '../../../core/models/master.model';

@Component({
  selector: 'app-manifest-problem',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './manifest-problem.html',
  styleUrl: './manifest-problem.css'
})
export class ManifestProblemComponent implements OnInit {
  private cargoService = inject(CargoService);
  private routePlanningService = inject(RoutePlanningService);

  problems = signal<any[]>([]);
  totalCount = signal(0);
  page = signal(1);
  pageSize = signal(10);
  searchQuery = signal('');
  statusFilter = signal('');

  isLoading = signal(false);

  // Resolve Modal State
  selectedProblemId = signal<number | null>(null);
  resolveReason = signal('');

  // View Details State
  viewProblem = signal<any | null>(null);
  viewPickupOrder = signal<any | null>(null);

  ngOnInit() {
    this.loadProblems();
  }

  loadProblems() {
    this.isLoading.set(true);
    this.cargoService.getManifestProblems(
      this.searchQuery() || undefined,
      this.statusFilter() || undefined,
      this.page(),
      this.pageSize()
    ).subscribe({
      next: (res) => {
        if (res.data) {
          this.problems.set(res.data);
          this.totalCount.set(res.pagination?.total_items || 0);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load manifest problems:', err);
        this.isLoading.set(false);
      }
    });
  }

  onSearch() {
    this.page.set(1);
    this.loadProblems();
  }

  onPageChange(newPage: number) {
    this.page.set(newPage);
    this.loadProblems();
  }

  openResolveModal(id: number) {
    this.selectedProblemId.set(id);
    this.resolveReason.set('');
    const modal = document.getElementById('resolve_modal') as HTMLDialogElement;
    if (modal) modal.showModal();
  }

  closeResolveModal() {
    this.selectedProblemId.set(null);
    this.resolveReason.set('');
    const modal = document.getElementById('resolve_modal') as HTMLDialogElement;
    if (modal) modal.close();
  }

  submitResolve() {
    const id = this.selectedProblemId();
    if (!id) return;
    if (!this.resolveReason().trim()) {
      alert('Resolution reason is required.');
      return;
    }

    this.cargoService.resolveManifestProblem(id, this.resolveReason()).subscribe({
      next: () => {
        this.closeResolveModal();
        this.loadProblems();
      },
      error: (err) => {
        alert('Failed to resolve problem: ' + err.message);
      }
    });
  }

  openViewModal(problem: any) {
    this.viewProblem.set(problem);
    this.viewPickupOrder.set(null);
    const modal = document.getElementById('view_modal') as HTMLDialogElement;
    if (modal) modal.showModal();

    if (problem.pickupOrderId) {
      this.routePlanningService.getPickupOrderById(problem.pickupOrderId).subscribe({
        next: (res) => {
          if (res.data) {
            this.viewPickupOrder.set(res.data);
          }
        },
        error: (err) => console.error('Failed to fetch pickup order details', err)
      });
    }
  }

  closeViewModal() {
    this.viewProblem.set(null);
    this.viewPickupOrder.set(null);
    const modal = document.getElementById('view_modal') as HTMLDialogElement;
    if (modal) modal.close();
  }
}
