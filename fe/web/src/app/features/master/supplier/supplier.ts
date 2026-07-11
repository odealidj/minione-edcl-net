import { Component, OnInit, inject, ViewChild, ElementRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Supplier } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { MasterDataService } from '../../../core/services/master-data.service';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-supplier',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PageHeaderComponent, PaginationComponent, SearchBarComponent, CardComponent],
  templateUrl: './supplier.html',
  styleUrl: './supplier.css'
})
export class SupplierComponent implements OnInit {
  items = signal<Supplier[]>([]);
  meta = signal<PaginationMeta | null>(null);
  isLoading = signal(true);
  searchQuery = '';
  currentPage = 1;
  pageSize = 10;

  @ViewChild('crudModal') crudModal!: ElementRef<HTMLDialogElement>;
  form: FormGroup;
  isEditMode = false;
  editingId: number | null = null;
  isSaving = false;

  private service = inject(MasterDataService);
  private fb = inject(FormBuilder);

  constructor() {
    this.form = this.fb.group({
      supplierCode: ['', Validators.required],
      name: ['', Validators.required],
      address: [''],
      latitude: [null],
      longitude: [null],
      geofenceRadiusMeters: [null]
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.service.getSuppliers(this.searchQuery, this.currentPage, this.pageSize).subscribe({
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

  openModal(item?: Supplier): void {
    if (item) {
      this.isEditMode = true;
      this.editingId = item.id;
      this.form.patchValue({ 
        supplierCode: item.supplierCode, 
        name: item.name, 
        address: item.address,
        latitude: item.latitude,
        longitude: item.longitude,
        geofenceRadiusMeters: item.geofenceRadiusMeters
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

    const lat = val.latitude ? Number(val.latitude) : null;
    const lng = val.longitude ? Number(val.longitude) : null;
    const radius = val.geofenceRadiusMeters ? Number(val.geofenceRadiusMeters) : null;

    if (this.isEditMode && this.editingId) {
      this.service.updateSupplier(this.editingId, val.supplierCode, val.name, val.address, lat, lng, radius).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    } else {
      this.service.createSupplier(val.supplierCode, val.name, val.address, lat, lng, radius).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.currentPage = 1; this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    }
  }

  deleteItem(id: number): void {
    if (confirm('Are you sure you want to delete this item?')) {
      this.service.deleteSupplier(id).subscribe({
        next: () => this.loadData(),
        error: (err) => console.error(err)
      });
    }
  }
}
