import { Component, OnInit, inject, ViewChild, ElementRef, signal, ChangeDetectorRef, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { MasterDataService } from '../../../core/services/master-data.service';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Truck, Transporter, Driver } from '../../../core/models/master.model';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { CardComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'app-truck-assignment',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PageHeaderComponent, SearchBarComponent, CardComponent],
  templateUrl: './truck-assignment.html',
})
export class TruckAssignment implements OnInit {
  items = signal<any[]>([]);
  transporters = signal<Transporter[]>([]);
  trucks = signal<Truck[]>([]);
  drivers = signal<Driver[]>([]);
  isLoading = signal(true);
  searchQuery = '';
  
  selectedIds = signal<Set<string>>(new Set());
  isDeletingSelected = signal(false);
  
  @ViewChild('assignModal') assignModal!: ElementRef<HTMLDialogElement>;
  assignForm: FormGroup;
  isSaving = false;

  private service = inject(MasterDataService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  constructor() {
    this.assignForm = this.fb.group({
      transporterId: [null, Validators.required],
      truckId: [{ value: null, disabled: true }, Validators.required],
      driverId: [{ value: null, disabled: true }, Validators.required]
    });

    this.assignForm.get('transporterId')?.valueChanges.subscribe(tId => {
      this.assignForm.patchValue({ truckId: null, driverId: null }, { emitEvent: false });
      if (tId) {
        this.assignForm.get('truckId')?.enable();
        this.assignForm.get('driverId')?.enable();
        this.loadTrucks(tId);
        this.loadDrivers(tId);
      } else {
        this.assignForm.get('truckId')?.disable();
        this.assignForm.get('driverId')?.disable();
        this.trucks.set([]);
        this.drivers.set([]);
      }
    });
  }

  ngOnInit() {
    this.loadTransporters();
    this.loadData();
  }

  loadTransporters(): void {
    this.service.getTransporters().subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.transporters.set(res.data);
        }
      },
      error: (err) => console.error('Failed to load transporters', err)
    });
  }

  loadTrucks(transporterId: number): void {
    this.service.getAvailableTrucks(transporterId).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.trucks.set(res.data);
        }
      }
    });
  }

  loadDrivers(transporterId: number): void {
    this.service.getAvailableDrivers(transporterId).subscribe({
      next: (res) => {
        if (res.status === 'success') {
          this.drivers.set(res.data);
        }
      }
    });
  }

  loadData() {
    this.isLoading.set(true);
    this.service.getTruckAssignments().subscribe({
      next: (res) => {
        this.items.set(res.data || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Truck assignments error:', err);
        this.isLoading.set(false);
      }
    });
  }

  onSearch(query: string): void {
    this.searchQuery = query;
  }

  get filteredAssignments() {
    const data = this.items();
    if (!this.searchQuery) return data;
    const lowerQ = this.searchQuery.toLowerCase();
    return data.filter(a => 
      (a.plateNumber && a.plateNumber.toLowerCase().includes(lowerQ)) ||
      (a.driverName && a.driverName.toLowerCase().includes(lowerQ)) ||
      (a.transporterName && a.transporterName.toLowerCase().includes(lowerQ))
    );
  }

  openAssignModal(): void {
    this.assignForm.reset();
    this.assignForm.get('truckId')?.disable();
    this.assignForm.get('driverId')?.disable();
    this.trucks.set([]);
    this.drivers.set([]);
    this.assignModal.nativeElement.showModal();
  }

  closeAssignModal(): void {
    this.assignModal.nativeElement.close();
  }

  saveAssign(): void {
    if (this.assignForm.invalid) return;
    this.isSaving = true;
    const val = this.assignForm.value;
    
    this.service.assignDriverToTruck(val.truckId, val.driverId).subscribe({
      next: () => {
        this.isSaving = false;
        this.cdr.detectChanges();
        this.closeAssignModal();
        this.loadData();
      },
      error: (err) => {
        console.error(err);
        this.isSaving = false;
        this.cdr.detectChanges();
        alert('Failed to assign driver: ' + (err.error?.message || err.message));
      }
    });
  }

  unassign(truckId: number, driverId: number) {
    if (confirm('Are you sure you want to unassign this driver from the truck?')) {
      this.service.unassignDriverFromTruck(truckId, driverId).subscribe({
        next: () => {
          this.loadData();
        },
        error: (err) => {
          this.cdr.detectChanges();
          alert('Failed to unassign: ' + (err.error?.message || err.message));
        }
      });
    }
  }

  toggleSelection(truckId: number, driverId: number): void {
    const id = `${truckId}-${driverId}`;
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
      this.selectedIds.set(new Set(this.filteredAssignments.map(i => `${i.truckId}-${i.driverId}`)));
    } else {
      this.selectedIds.set(new Set());
    }
  }

  isAllSelected(): boolean {
    return this.filteredAssignments.length > 0 && this.selectedIds().size === this.filteredAssignments.length;
  }

  isSelected(truckId: number, driverId: number): boolean {
    return this.selectedIds().has(`${truckId}-${driverId}`);
  }

  deleteSelected(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;
    
    if (confirm(`Are you sure you want to unassign ${ids.length} selected items?`)) {
      this.isDeletingSelected.set(true);
      const requests = ids.map(id => {
        const [truckIdStr, driverIdStr] = id.split('-');
        return this.service.unassignDriverFromTruck(Number(truckIdStr), Number(driverIdStr));
      });
      
      forkJoin(requests).subscribe({
        next: () => {
          this.isDeletingSelected.set(false);
          this.selectedIds.set(new Set());
          this.cdr.detectChanges();
          this.loadData();
        },
        error: (err) => {
          console.error('Error unassigning selected items', err);
          this.isDeletingSelected.set(false);
          this.cdr.detectChanges();
          alert('Failed to unassign some items.');
        }
      });
    }
  }
}
