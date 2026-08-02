import { Component, OnInit, inject, ViewChild, ElementRef, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Driver, LogisticPartner } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { MasterDataService } from '../../../core/services/master-data.service';
import { AdminService } from '../../../core/services/admin.service';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-driver',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PageHeaderComponent, PaginationComponent, SearchBarComponent, CardComponent],
  templateUrl: './driver.html',
  styleUrl: './driver.css'
})
export class DriverComponent implements OnInit {
  items = signal<Driver[]>([]);
  meta = signal<PaginationMeta | null>(null);
  isLoading = signal(true);
  logisticPartners = signal<LogisticPartner[]>([]);
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

  private service = inject(MasterDataService);
  private adminService = inject(AdminService);
  private fb = inject(FormBuilder);

  constructor() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      nik: ['', Validators.required],
      phoneNumber: [''],
      logisticPartnerId: [null],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.loadData();
    this.loadLogisticPartners();
  }

  loadLogisticPartners(): void {
    this.adminService.getLogisticPartners('', 1, 1000).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.logisticPartners.set(res.data);
        }
      },
      error: (err) => console.error('Failed to load logisticPartners', err)
    });
  }

  loadData(): void {
    this.isLoading.set(true);
    this.service.getDrivers(this.searchQuery, this.currentPage, this.pageSize).subscribe({
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
      const requests = ids.map(id => this.service.deleteDriver(id));
      
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

  openModal(item?: Driver): void {
    if (item) {
      this.isEditMode = true;
      this.editingId = item.id;
      this.form.patchValue({ name: item.name, nik: item.nik, phoneNumber: item.phoneNumber, logisticPartnerId: item.logisticPartnerId, isActive: item.isActive !== false });
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

    // Convert string to number if not null
    const logisticPartnerId = val.logisticPartnerId ? Number(val.logisticPartnerId) : null;

    if (this.isEditMode && this.editingId) {
      this.service.updateDriver(this.editingId, val.name, val.nik, val.phoneNumber, logisticPartnerId, val.isActive).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    } else {
      this.service.createDriver(val.name, val.nik, val.phoneNumber, logisticPartnerId, val.isActive).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.currentPage = 1; this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    }
  }

  deleteItem(id: number): void {
    if (confirm('Are you sure you want to delete this item?')) {
      this.service.deleteDriver(id).subscribe({
        next: () => this.loadData(),
        error: (err) => console.error(err)
      });
    }
  }

  getLogisticPartnerName(id?: number | null): string {
    if (!id) return '-';
    const lp = this.logisticPartners().find(x => x.id === id);
    return lp ? lp.name : '-';
  }
}
