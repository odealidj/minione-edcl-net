import { Component, OnInit, inject, ViewChild, ElementRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { Route, LogisticPartner } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { AdminService } from '../../../core/services/admin.service';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-route',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PageHeaderComponent, PaginationComponent, SearchBarComponent, CardComponent],
  templateUrl: './route.html',
  styleUrls: ['./route.css']
})
export class RouteComponent implements OnInit {
  items = signal<Route[]>([]);
  partners = signal<LogisticPartner[]>([]);
  meta = signal<PaginationMeta | null>(null);
  isLoading = signal(true);
  searchQuery = '';
  currentPage = 1;
  pageSize = 10;
  
  selectedIds = signal<Set<number>>(new Set());
  isDeletingSelected = signal(false);

  @ViewChild('crudModal') crudModal!: ElementRef<HTMLDialogElement>;
  form: FormGroup;
  isEditMode = false;
  editingId: number | null = null;
  isSaving = false;

  private service = inject(AdminService);
  private fb = inject(FormBuilder);

  constructor() {
    this.form = this.fb.group({
      routeCode: [''],
      cycleCode: [''],
      logisticPartnerId: [null]
    });
  }

  ngOnInit(): void {
    this.loadData();
    this.loadPartners();
  }

  loadPartners(): void {
    this.service.getLogisticPartners('', 1, 100).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.partners.set(res.data);
        }
      },
      error: (err) => console.error('Failed to load partners', err)
    });
  }

  loadData(): void {
    this.isLoading.set(true);
    this.service.getRoutes(this.searchQuery, this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.items.set(res.data);
          this.meta.set(res.pagination);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load data', err);
        this.isLoading.set(false);
      }
    });
  }

  onSearch(query: string): void {
    this.searchQuery = query;
    this.currentPage = 1;
    this.loadData();
  }

  changePage(page: number): void {
    this.currentPage = page;
    this.loadData();
  }

  changePageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadData();
  }

  toggleSelection(id: number): void {
    const current = new Set(this.selectedIds());
    if (current.has(id)) {
      current.delete(id);
    } else {
      current.add(id);
    }
    this.selectedIds.set(current);
  }

  toggleAll(event: Event): void {
    const isChecked = (event.target as HTMLInputElement).checked;
    if (isChecked) {
      this.selectedIds.set(new Set(this.items().map(i => i.id)));
    } else {
      this.selectedIds.set(new Set());
    }
  }

  isAllSelected(): boolean {
    return this.items().length > 0 && this.selectedIds().size === this.items().length;
  }

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  deleteSelected(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;
    
    if (confirm(`Are you sure you want to delete ${ids.length} selected items?`)) {
      this.isDeletingSelected.set(true);
      const requests = ids.map(id => this.service.deleteRoute(id));
      
      forkJoin(requests).subscribe({
        next: () => {
          this.isDeletingSelected.set(false);
          this.selectedIds.set(new Set());
          this.loadData();
        },
        error: (err) => {
          console.error('Error deleting selected items', err);
          this.isDeletingSelected.set(false);
        }
      });
    }
  }

  openModal(item?: Route): void {
    if (item) {
      this.isEditMode = true;
      this.editingId = item.id;
      this.form.patchValue({ 
        routeCode: item.routeCode, 
        cycleCode: item.cycleCode,
        logisticPartnerId: item.logisticPartnerId || null
      });
    } else {
      this.isEditMode = false;
      this.editingId = null;
      this.form.reset();
    }
    this.crudModal.nativeElement.showModal();
  }

  closeModal(): void {
    this.crudModal.nativeElement.close();
  }

  save(): void {
    if (this.form.invalid) return;
    this.isSaving = true;
    const val = this.form.value;

    if (this.isEditMode && this.editingId) {
      this.service.updateRoute(this.editingId, val.routeCode, val.cycleCode, val.logisticPartnerId).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.loadData();
        },
        error: (err) => {
          console.error(err);
          this.isSaving = false;
        }
      });
    } else {
      this.service.createRoute(val.routeCode, val.cycleCode, val.logisticPartnerId).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.currentPage = 1;
          this.loadData();
        },
        error: (err) => {
          console.error(err);
          this.isSaving = false;
        }
      });
    }
  }

  deleteItem(id: number): void {
    if (confirm('Are you sure you want to delete this item?')) {
      this.service.deleteRoute(id).subscribe({
        next: () => this.loadData(),
        error: (err) => console.error(err)
      });
    }
  }
}
