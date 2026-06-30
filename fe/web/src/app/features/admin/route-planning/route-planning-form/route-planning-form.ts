import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { RoutePlanningService } from '../../../../core/services/route-planning.service';
import { MasterDataService } from '../../../../core/services/master-data.service';
import { CargoService } from '../../../../core/services/cargo.service';
import { Supplier, Driver, Truck } from '../../../../core/models/master.model';

@Component({
  selector: 'app-route-planning-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, DragDropModule],
  templateUrl: './route-planning-form.html'
})
export class RoutePlanningFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private routeService = inject(RoutePlanningService);
  private masterService = inject(MasterDataService);
  private cargoService = inject(CargoService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  form!: FormGroup;
  orderId: number | null = null;
  loading: boolean = false;
  saving: boolean = false;

  suppliers: Supplier[] = [];
  drivers: Driver[] = [];
  trucks: Truck[] = [];

  // Custom Dropdown State
  filteredDrivers: Driver[] = [];
  selectedDriverName: string = '';
  driverDropdownOpen: boolean = false;

  filteredTrucks: Truck[] = [];
  selectedTruckName: string = '';
  truckDropdownOpen: boolean = false;

  // Manifest Selection Modal
  showManifestModal: boolean = false;
  currentStopIndex: number = -1;
  availableManifests: any[] = [];
  selectedManifests: Set<number> = new Set();
  manifestLoading: boolean = false;

  ngOnInit() {
    this.initForm();
    this.loadMasterData();
    
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.orderId = +id;
        this.loadOrder(this.orderId);
      }
    });
  }

  initForm() {
    this.form = this.fb.group({
      poNo: ['', Validators.required],
      pickupDate: ['', Validators.required],
      routeCode: ['', Validators.required],
      cycleCode: ['', Validators.required],
      estimatedDepartureTime: ['08:00:00', Validators.required],
      driverId: [null],
      truckId: [null],
      stops: this.fb.array([])
    });
  }

  get stops() {
    return this.form.get('stops') as FormArray;
  }

  addStop(supplierId: number = 0, sequence: number = this.stops.length + 1) {
    const stopGroup = this.fb.group({
      supplierId: [supplierId, Validators.required],
      sequence: [sequence],
      manifests: this.fb.array([])
    });
    this.stops.push(stopGroup);
  }

  removeStop(index: number) {
    this.stops.removeAt(index);
    this.updateSequences();
  }

  getManifestsForStop(stopIndex: number) {
    return this.stops.at(stopIndex).get('manifests') as FormArray;
  }

  removeManifest(stopIndex: number, manifestIndex: number) {
    this.getManifestsForStop(stopIndex).removeAt(manifestIndex);
  }

  drop(event: CdkDragDrop<string[]>) {
    moveItemInArray(this.stops.controls, event.previousIndex, event.currentIndex);
    this.updateSequences();
  }

  updateSequences() {
    this.stops.controls.forEach((control, index) => {
      control.get('sequence')?.setValue(index + 1);
    });
  }

  loadMasterData() {
    this.masterService.getSuppliers('', 1, 1000).subscribe(res => this.suppliers = res.data);
    this.masterService.getDrivers('', 1, 1000).subscribe(res => {
      this.drivers = res.data;
      this.filteredDrivers = [...this.drivers];
      
      const currentDriverId = this.form.get('driverId')?.value;
      if (currentDriverId) {
        const d = this.drivers.find(x => x.id === currentDriverId);
        if (d) this.selectedDriverName = `${d.name} (${d.nik})`;
      }
    });
    this.masterService.getTrucks('', 1, 1000).subscribe(res => {
      this.trucks = res.data;
      this.filteredTrucks = [...this.trucks];

      const currentTruckId = this.form.get('truckId')?.value;
      if (currentTruckId) {
        const t = this.trucks.find(x => x.id === currentTruckId);
        if (t) this.selectedTruckName = `${t.plateNumber} (${t.vehicleType || '-'})`;
      }
    });
  }

  // Driver Dropdown Methods
  onDriverSearch(event: Event) {
    this.driverDropdownOpen = true;
    const term = (event.target as HTMLInputElement).value.toLowerCase();
    if (!term) {
      this.filteredDrivers = [...this.drivers];
    } else {
      this.filteredDrivers = this.drivers.filter(d => 
        d.name.toLowerCase().includes(term) || d.nik.toLowerCase().includes(term)
      );
    }
  }

  onDriverBlur() {
    // Delay slightly so that mousedown on options can fire before the dropdown is removed
    setTimeout(() => {
      this.driverDropdownOpen = false;
    }, 200);
  }

  selectDriver(driver: Driver | null) {
    if (driver) {
      this.form.patchValue({ driverId: driver.id });
      this.selectedDriverName = `${driver.name} (${driver.nik})`;
    } else {
      this.form.patchValue({ driverId: null });
      this.selectedDriverName = '';
    }
    this.driverDropdownOpen = false;
  }

  // Truck Dropdown Methods
  onTruckSearch(event: Event) {
    this.truckDropdownOpen = true;
    const term = (event.target as HTMLInputElement).value.toLowerCase();
    if (!term) {
      this.filteredTrucks = [...this.trucks];
    } else {
      this.filteredTrucks = this.trucks.filter(t => 
        t.plateNumber.toLowerCase().includes(term) || (t.vehicleType && t.vehicleType.toLowerCase().includes(term))
      );
    }
  }

  onTruckBlur() {
    setTimeout(() => {
      this.truckDropdownOpen = false;
    }, 200);
  }

  selectTruck(truck: Truck | null) {
    if (truck) {
      this.form.patchValue({ truckId: truck.id });
      this.selectedTruckName = `${truck.plateNumber} (${truck.vehicleType || '-'})`;
    } else {
      this.form.patchValue({ truckId: null });
      this.selectedTruckName = '';
    }
    this.truckDropdownOpen = false;
  }

  loadOrder(id: number) {
    this.loading = true;
    this.routeService.getPickupOrderById(id).subscribe({
      next: (res) => {
        const order = res.data;
        this.form.patchValue({
          poNo: order.poNo,
          pickupDate: order.pickupDate.split('T')[0],
          routeCode: order.routeCode,
          cycleCode: order.cycleCode,
          estimatedDepartureTime: order.estimatedDepartureTime,
          driverId: order.driverId,
          truckId: order.truckId
        });

        // Try to set the selected driver name if drivers are already loaded
        if (order.driverId && this.drivers.length > 0) {
          const d = this.drivers.find(x => x.id === order.driverId);
          if (d) this.selectedDriverName = `${d.name} (${d.nik})`;
        }

        // Try to set the selected truck name if trucks are already loaded
        if (order.truckId && this.trucks.length > 0) {
          const t = this.trucks.find(x => x.id === order.truckId);
          if (t) this.selectedTruckName = `${t.plateNumber} (${t.vehicleType || '-'})`;
        }

        // Load stops
        this.stops.clear();
        order.details?.forEach(stop => {
          const stopGroup = this.fb.group({
            supplierId: [stop.supplierId, Validators.required],
            sequence: [stop.sequence],
            manifests: this.fb.array([])
          });
          const manifestsArray = stopGroup.get('manifests') as FormArray;
          stop.manifests?.forEach(m => {
            manifestsArray.push(this.fb.group({
              manifestNo: [m.manifestNo, Validators.required],
              totalKanban: [m.totalKanban, Validators.required],
              orderType: [m.orderType],
              totalSkid: [m.totalSkid],
              dockCode: [m.dockCode]
            }));
          });
          this.stops.push(stopGroup);
        });
        
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        alert('Failed to load order');
      }
    });
  }

  openManifestModal(stopIndex: number) {
    this.currentStopIndex = stopIndex;
    this.showManifestModal = true;
    this.selectedManifests.clear();
    this.manifestLoading = true;
    // Fetch manifests from cargo service. For simplicity fetching all first page
    this.cargoService.getManifests().subscribe(res => {
      this.availableManifests = res.data;
      this.manifestLoading = false;
    });
  }

  closeManifestModal() {
    this.showManifestModal = false;
    this.currentStopIndex = -1;
  }

  toggleManifestSelection(manifest: any) {
    if (this.selectedManifests.has(manifest.id)) {
      this.selectedManifests.delete(manifest.id);
    } else {
      this.selectedManifests.add(manifest.id);
    }
  }

  confirmManifestSelection() {
    const manifestsArray = this.getManifestsForStop(this.currentStopIndex);
    this.availableManifests.forEach(m => {
      if (this.selectedManifests.has(m.id)) {
        manifestsArray.push(this.fb.group({
          manifestNo: [m.manifestNo, Validators.required],
          totalKanban: [m.totalKanban, Validators.required],
          orderType: ['ORG'],
          totalSkid: [1],
          dockCode: ['-']
        }));
      }
    });
    this.closeManifestModal();
  }
  
  // Also provide a manual add option
  addManualManifest(stopIndex: number) {
    const manifestsArray = this.getManifestsForStop(stopIndex);
    manifestsArray.push(this.fb.group({
      manifestNo: ['', Validators.required],
      totalKanban: [1, Validators.required],
      orderType: ['ORG'],
      totalSkid: [1],
      dockCode: ['-']
    }));
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    
    this.saving = true;
    const command = this.form.value;
    
    if (this.orderId) {
      this.routeService.updatePickupOrder(this.orderId, { id: this.orderId, ...command }).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/admin/route-planning']);
        },
        error: () => this.saving = false
      });
    } else {
      this.routeService.createPickupOrder(command).subscribe({
        next: () => {
          this.saving = false;
          this.router.navigate(['/admin/route-planning']);
        },
        error: () => this.saving = false
      });
    }
  }
}
