import { Component, OnInit, inject, ViewChild, ElementRef, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { LogisticPartner, GpsVendor } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { AdminService } from '../../../core/services/admin.service';
import { MasterDataService } from '../../../core/services/master-data.service';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-logisticPartner',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PageHeaderComponent, PaginationComponent, SearchBarComponent, CardComponent],
  templateUrl: './logistic-partner.html',
  styleUrls: ['./logistic-partner.css']
})
export class LogisticPartnerComponent implements OnInit {
  items = signal<LogisticPartner[]>([]);
  meta = signal<PaginationMeta | null>(null);
  isLoading = signal(true);
  searchQuery = '';
  currentPage = 1;
  pageSize = 10;
  gpsVendors = signal<GpsVendor[]>([]);
  
  selectedIds = signal<Set<number>>(new Set());
  isDeletingSelected = signal(false);

  @ViewChild('crudModal') crudModal!: ElementRef<HTMLDialogElement>;
  @ViewChild('assignModal') assignModal!: ElementRef<HTMLDialogElement>;
  
  form: FormGroup;
  isEditMode = false;
  editingId: number | null = null;
  isSaving = false;
  
  assigningItem = signal<LogisticPartner | null>(null);
  assignGpsVendorIds = signal<Set<number>>(new Set());
  isAssignSaving = signal(false);

  private service = inject(AdminService);
  private masterDataService = inject(MasterDataService);
  private fb = inject(FormBuilder);

  constructor() {
    this.form = this.fb.group({
      code: [''],
      name: ['']
    });
  }

  ngOnInit(): void {
    this.loadData();
    this.loadGpsVendors();
  }

  loadGpsVendors(): void {
    this.masterDataService.getGpsVendors('', 1, 100).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.gpsVendors.set(res.data);
        }
      },
      error: (err) => console.error('Failed to load GPS Vendors', err)
    });
  }

  loadData(): void {
    this.isLoading.set(true);
    this.service.getLogisticPartners(this.searchQuery, this.currentPage, this.pageSize).subscribe({
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
      const requests = ids.map(id => this.service.deleteLogisticPartner(id));
      
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

  getSyncStatusBadgeClass(status?: string): string {
    if (!status) return 'badge-ghost';
    const s = status.toLowerCase();
    if (s === 'success') return 'badge-success text-success-content';
    if (s === 'failed' || s === 'error') return 'badge-error text-error-content';
    return 'badge-ghost';
  }

  openModal(item?: LogisticPartner): void {
    if (item) {
      this.isEditMode = true;
      this.editingId = item.id;
      this.form.patchValue({
        code: item.code,
        name: item.name
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
    this.form.reset();
  }

  getVendorNames(vendorIds?: number[]): string {
    if (!vendorIds || vendorIds.length === 0) return '-';
    const names = vendorIds.map(id => this.gpsVendors().find(v => v.id === id)?.name).filter(Boolean);
    return names.join(', ');
  }

  openAssignModal(item: LogisticPartner): void {
    this.assigningItem.set(item);
    this.assignGpsVendorIds.set(new Set(item.gpsVendorIds || []));
    this.assignModal.nativeElement.showModal();
  }

  closeAssignModal(): void {
    this.assignModal.nativeElement.close();
    this.assigningItem.set(null);
  }

  toggleGpsVendor(vendorId: number, event: any): void {
    const isChecked = event.target.checked;
    const current = new Set(this.assignGpsVendorIds());
    if (isChecked) {
      current.add(vendorId);
    } else {
      current.delete(vendorId);
    }
    this.assignGpsVendorIds.set(current);
  }

  saveAssign(): void {
    const item = this.assigningItem();
    if (!item) return;

    this.isAssignSaving.set(true);
    this.service.updateLogisticPartner(item.id, item.code, item.name, Array.from(this.assignGpsVendorIds())).subscribe({
      next: () => {
        this.isAssignSaving.set(false);
        this.closeAssignModal();
        this.loadData();
      },
      error: (err) => {
        console.error('Assign failed', err);
        this.isAssignSaving.set(false);
      }
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.isSaving = true;
    const v = this.form.value;

    if (this.isEditMode && this.editingId) {
      this.service.updateLogisticPartner(this.editingId, v.code, v.name, this.items().find(x => x.id === this.editingId)?.gpsVendorIds || []).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.loadData();
        },
        error: (err) => {
          console.error('Update failed', err);
          this.isSaving = false;
        }
      });
    } else {
      this.service.createLogisticPartner(v.code, v.name, []).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeModal();
          this.loadData();
        },
        error: (err) => {
          console.error('Create failed', err);
          this.isSaving = false;
        }
      });
    }
  }

  deleteItem(id: number): void {
    if (confirm('Are you sure you want to delete this item?')) {
      this.service.deleteLogisticPartner(id).subscribe({
        next: () => this.loadData(),
        error: (err) => console.error(err)
      });
    }
  }
}
