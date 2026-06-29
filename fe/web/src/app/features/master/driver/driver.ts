import { Component, OnInit, inject, ViewChild, ElementRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Driver } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { MasterDataService } from '../../../core/services/master-data.service';

@Component({
  selector: 'app-driver',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './driver.html',
  styleUrl: './driver.css'
})
export class DriverComponent implements OnInit {
  items = signal<Driver[]>([]);
  meta = signal<PaginationMeta | null>(null);
  isLoading = signal(true);
  searchQuery = '';
  currentPage = 1;
  pageSize = 10;

  @ViewChild('crudModal') crudModal!: ElementRef<HTMLDialogElement>;
  form: FormGroup;
  isEditMode = false;
  editingId: string | null = null;
  isSaving = false;

  private service = inject(MasterDataService);
  private fb = inject(FormBuilder);

  constructor() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      nik: ['', Validators.required],
      phoneNumber: ['']
    });
  }

  ngOnInit(): void {
    this.loadData();
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

  openModal(item?: Driver): void {
    if (item) {
      this.isEditMode = true;
      this.editingId = item.id;
      this.form.patchValue({ name: item.name, nik: item.nik, phoneNumber: item.phoneNumber });
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
      this.service.updateDriver(this.editingId, val.name, val.nik, val.phoneNumber).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    } else {
      this.service.createDriver(val.name, val.nik, val.phoneNumber).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.currentPage = 1; this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    }
  }

  deleteItem(id: string): void {
    if (confirm('Are you sure you want to delete this item?')) {
      this.service.deleteDriver(id).subscribe({
        next: () => this.loadData(),
        error: (err) => console.error(err)
      });
    }
  }
}
