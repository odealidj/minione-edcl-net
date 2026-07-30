import { Component, OnInit, inject, ViewChild, ElementRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { GpsVendor } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { MasterDataService } from '../../../core/services/master-data.service';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-gps-vendor',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PageHeaderComponent, PaginationComponent, SearchBarComponent, CardComponent],
  templateUrl: './gps-vendor.html',
  styleUrls: ['./gps-vendor.css']
})
export class GpsVendorComponent implements OnInit {
  items = signal<GpsVendor[]>([]);
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
  testingId = signal<number | null>(null);

  private service = inject(MasterDataService);
  private fb = inject(FormBuilder);

  constructor() {
    this.form = this.fb.group({
      code: ['', Validators.required],
      name: ['', Validators.required],
      providerType: [1, Validators.required],
      apiUrl: [''],
      apiUsername: [''],
      apiPassword: [''], // Note: Not editing password on update unless filled
      apiToken: ['']
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.service.getGpsVendors(this.searchQuery, this.currentPage, this.pageSize).subscribe({
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

  openModal(item?: GpsVendor): void {
    if (item) {
      this.isEditMode = true;
      this.editingId = item.id;
      this.form.patchValue({
        code: item.code,
        name: item.name,
        providerType: item.providerType,
        apiUrl: item.apiUrl,
        apiUsername: item.apiUsername,
        apiPassword: '', // Don't show existing password
        apiToken: item.apiToken
      });
    } else {
      this.isEditMode = false;
      this.editingId = null;
      this.form.reset({ providerType: 1 });
    }
    this.crudModal.nativeElement.showModal();
  }

  closeModal(): void {
    this.crudModal.nativeElement.close();
    this.form.reset({ providerType: 1 });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const v = this.form.value;

    if (this.isEditMode && this.editingId) {
      this.service.updateGpsVendor(this.editingId, v.code, v.name, v.providerType, v.apiUrl, v.apiUsername, v.apiPassword, v.apiToken).subscribe({
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
      this.service.createGpsVendor(v.code, v.name, v.providerType, v.apiUrl, v.apiUsername, v.apiPassword, v.apiToken).subscribe({
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
    if (confirm('Are you sure you want to delete this vendor?')) {
      this.service.deleteGpsVendor(id).subscribe({
        next: () => {
          this.loadData();
        },
        error: (err) => {
          console.error('Delete failed', err);
        }
      });
    }
  }

  testConnection(item: GpsVendor): void {
    if (this.testingId() === item.id) return;
    
    this.testingId.set(item.id);
    this.service.testGpsConnection(item.id).subscribe({
      next: (res) => {
        this.testingId.set(null);
        // Refresh data to get updated status and last checked time
        this.loadData();
      },
      error: (err) => {
        this.testingId.set(null);
        alert('GPS connection test failed.');
        this.loadData();
      }
    });
  }
}
