import { Component, OnInit, inject, ViewChild, ElementRef, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Truck } from '../../../core/models/master.model';
import { PaginationMeta } from '../../../core/models/api.model';
import { MasterDataService } from '../../../core/services/master-data.service';

@Component({
  selector: 'app-truck',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './truck.html',
  styleUrl: './truck.css'
})
export class TruckComponent implements OnInit {
  items = signal<Truck[]>([]);
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
      plateNumber: ['', Validators.required],
      truckType: ['', Validators.required],
      truckCapacity: [0, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.service.getTrucks(this.searchQuery, this.currentPage, this.pageSize).subscribe({
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

  openModal(item?: Truck): void {
    if (item) {
      this.isEditMode = true;
      this.editingId = item.id;
      this.form.patchValue({ plateNumber: item.plateNumber, truckType: item.truckType, truckCapacity: item.truckCapacity });
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
      this.service.updateTruck(this.editingId, val.plateNumber, val.truckType, val.truckCapacity).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    } else {
      this.service.createTruck(val.plateNumber, val.truckType, val.truckCapacity).subscribe({
        next: () => { this.isSaving = false; this.closeModal(); this.currentPage = 1; this.loadData(); },
        error: (err) => { console.error(err); this.isSaving = false; }
      });
    }
  }

  deleteItem(id: string): void {
    if (confirm('Are you sure you want to delete this item?')) {
      this.service.deleteTruck(id).subscribe({
        next: () => this.loadData(),
        error: (err) => console.error(err)
      });
    }
  }
}
