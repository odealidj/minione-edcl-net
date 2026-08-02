import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { filter, switchMap, tap, catchError, distinctUntilChanged } from 'rxjs/operators';
import { of } from 'rxjs';
import { RoutePlanningService } from '../../../../core/services/route-planning.service';
import { MasterDataService } from '../../../../core/services/master-data.service';
import { CargoService } from '../../../../core/services/cargo.service';
import { Supplier, Driver, Truck, Route } from '../../../../core/models/master.model';

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
  private cdr = inject(ChangeDetectorRef);

  form!: FormGroup;
  orderId: number | null = null;
  loading: boolean = false;
  saving: boolean = false;

  suppliers: Supplier[] = [];
  allSuppliers: Supplier[] = []; // Store all to map names properly if needed
  drivers: Driver[] = [];
  trucks: Truck[] = [];
  truckAssignments: any[] = [];

  // Custom Dropdown State
  filteredDrivers: Driver[] = [];
  selectedDriverName: string = '';
  driverDropdownOpen: boolean = false;

  filteredTrucks: Truck[] = [];
  selectedTruckName: string = '';
  truckDropdownOpen: boolean = false;

  // Auto-fill tracking
  isDriverAutoFilled: boolean = false;
  isTruckAutoFilled: boolean = false;

  routes: Route[] = [];
  filteredRoutes: Route[] = [];
  selectedRouteName: string = '';
  routeDropdownOpen: boolean = false;

  // Local UI state for each stop
  stopUIStates: {
    page: number;
    pageSize: number;
    search: string;
    selectedCheckboxes: Set<number>;
    isLoading?: boolean;
    supplierDropdownOpen?: boolean;
    supplierSearchText?: string;
    supplierName?: string;
  }[] = [];

  // Manifest Selection Modal
  showManifestModal: boolean = false;
  currentStopIndex: number = -1;
  availableManifests: any[] = [];
  selectedManifests: Set<number> = new Set();
  manifestLoading: boolean = false;
  
  manifestModalState = {
    search: '',
    page: 1,
    pageSize: 10,
    totalPages: 1,
    supplierCode: ''
  };

  isViewMode: boolean = false;

  ngOnInit() {
    this.initForm();
    this.loadMasterData();
    
    this.route.queryParamMap.subscribe(q => {
      this.isViewMode = q.get('mode') === 'view';
      if (this.isViewMode) {
        this.form.disable();
      }
    });
    
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.orderId = +id;
        this.loadOrder(this.orderId);
      }
    });
  }

  initForm() {
    const now = new Date();
    const today = now.toISOString().split('T')[0];
    
    now.setHours(now.getHours() + 1);
    const estTime = now.toTimeString().split(' ')[0];

    this.form = this.fb.group({

      pickupDate: [today, Validators.required],
      routeCode: ['', Validators.required],
      cycleCode: ['', Validators.required],
      estimatedDepartureTime: [estTime, Validators.required],
      driverId: [null],
      truckId: [null],
      stops: this.fb.array([])
    });
  }

  get stops() {
    return this.form.get('stops') as FormArray;
  }

  addStop(supplierId: number | null = null, sequence: number = this.stops.length + 1) {
    const stopGroup = this.fb.group({
      supplierId: [supplierId, Validators.required],
      sequence: [sequence],
      manifests: this.fb.array([])
    });
    
    this.setupStopSupplierSubscription(stopGroup);
    
    this.stopUIStates.push({
      page: 1,
      pageSize: 5,
      search: '',
      selectedCheckboxes: new Set<number>(),
      isLoading: false,
      supplierDropdownOpen: false,
      supplierSearchText: '',
      supplierName: ''
    });
    
    this.stops.push(stopGroup);
  }

  setupStopSupplierSubscription(stopGroup: FormGroup) {
    stopGroup.get('supplierId')?.valueChanges.pipe(
      distinctUntilChanged(),
      filter(newSupplierId => !!newSupplierId),
      tap(() => {
        const stopIndex = this.stops.controls.indexOf(stopGroup);
        if (this.stopUIStates[stopIndex]) {
          this.stopUIStates[stopIndex].isLoading = true;
          const manifestsArray = stopGroup.get('manifests') as FormArray;
          manifestsArray.clear();
        }
      }),
      switchMap(newSupplierId => {
        const supplier = this.allSuppliers.find(s => s.id === newSupplierId) || this.suppliers.find(s => s.id === newSupplierId);
        if (!supplier) return of({ data: [] });
        return this.cargoService.getManifests('', supplier.supplierCode, 'Pending', false, 1, 1000).pipe(
          catchError(() => of({ data: [] }))
        );
      })
    ).subscribe((res: any) => {
      const stopIndex = this.stops.controls.indexOf(stopGroup);
      if (this.stopUIStates[stopIndex]) {
        const manifestsArray = stopGroup.get('manifests') as FormArray;
        res.data.forEach((m: any) => {
          manifestsArray.push(this.fb.group({
            manifestNo: [m.manifestNo, Validators.required],
            totalKanban: [m.totalKanbans, Validators.required],
            totalPart: [m.totalParts || 0],
            orderType: ['ORG'],
            totalSkid: [1],
            dockCode: ['-']
          }));
        });
        this.stopUIStates[stopIndex].isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  removeStop(index: number) {
    this.stops.removeAt(index);
    this.stopUIStates.splice(index, 1);
    this.updateSequences();
  }

  getVisibleManifests(stopIndex: number) {
    const manifestsArray = this.getManifestsForStop(stopIndex);
    const state = this.stopUIStates[stopIndex];
    if (!state) return { items: [], totalCount: 0, totalPages: 1 };
    
    let filtered = manifestsArray.controls.map((control, idx) => ({ control, originalIndex: idx }));
    
    if (state.search) {
      const term = state.search.toLowerCase();
      filtered = filtered.filter(item => {
        const val = item.control.value;
        return val.manifestNo?.toLowerCase().includes(term);
      });
    }
    
    const start = (state.page - 1) * state.pageSize;
    const paginated = filtered.slice(start, start + state.pageSize);
    
    return {
      items: paginated,
      totalCount: filtered.length,
      totalPages: Math.ceil(filtered.length / state.pageSize) || 1
    };
  }

  getStopSummary(stopIndex: number) {
    const manifestsArray = this.getManifestsForStop(stopIndex);
    let totalManifest = manifestsArray.length;
    let totalKanban = 0;
    let totalPart = 0;
    
    manifestsArray.controls.forEach(c => {
      totalKanban += +(c.value.totalKanban || 0);
      totalPart += +(c.value.totalPart || 0);
    });
    
    return { totalManifest, totalKanban, totalPart };
  }

  toggleManifestCheckbox(stopIndex: number, manifestIndex: number) {
    const state = this.stopUIStates[stopIndex];
    if (state.selectedCheckboxes.has(manifestIndex)) {
      state.selectedCheckboxes.delete(manifestIndex);
    } else {
      state.selectedCheckboxes.add(manifestIndex);
    }
  }

  toggleSelectAllManifests(stopIndex: number, event: any) {
    const state = this.stopUIStates[stopIndex];
    const visible = this.getVisibleManifests(stopIndex);
    if (event.target.checked) {
      visible.items.forEach(m => state.selectedCheckboxes.add(m.originalIndex));
    } else {
      visible.items.forEach(m => state.selectedCheckboxes.delete(m.originalIndex));
    }
  }

  bulkDeleteManifests(stopIndex: number) {
    const manifestsArray = this.getManifestsForStop(stopIndex);
    const state = this.stopUIStates[stopIndex];
    
    const indicesToDelete = Array.from(state.selectedCheckboxes).sort((a, b) => b - a);
    indicesToDelete.forEach(idx => {
      manifestsArray.removeAt(idx);
    });
    
    state.selectedCheckboxes.clear();
    
    // adjust page if necessary
    const visible = this.getVisibleManifests(stopIndex);
    if (state.page > visible.totalPages && visible.totalPages > 0) {
      state.page = visible.totalPages;
    }
  }

  onStopManifestSearch(stopIndex: number, term: string) {
    if (this.stopUIStates[stopIndex]) {
      this.stopUIStates[stopIndex].search = term;
      this.stopUIStates[stopIndex].page = 1;
    }
  }

  onStopManifestPageChange(stopIndex: number, newPage: number) {
    const state = this.stopUIStates[stopIndex];
    const totalPages = this.getVisibleManifests(stopIndex).totalPages;
    if (state && newPage >= 1 && newPage <= totalPages) {
      state.page = newPage;
    }
  }

  getManifestsForStop(stopIndex: number) {
    return this.stops.at(stopIndex).get('manifests') as FormArray;
  }

  removeManifest(stopIndex: number, manifestIndex: number) {
    this.getManifestsForStop(stopIndex).removeAt(manifestIndex);
  }

  drop(event: CdkDragDrop<string[]>) {
    moveItemInArray(this.stops.controls, event.previousIndex, event.currentIndex);
    moveItemInArray(this.stopUIStates, event.previousIndex, event.currentIndex);
    this.updateSequences();
  }

  updateSequences() {
    this.stops.controls.forEach((control, index) => {
      control.get('sequence')?.setValue(index + 1);
    });
  }

  loadMasterData() {
    this.masterService.getSuppliers('', 1, 1000).subscribe(res => {
      this.allSuppliers = res.data;
      
      // Fetch pending suppliers to filter the dropdown
      this.cargoService.getPendingManifestSuppliers().subscribe(pendingRes => {
        const pendingCodes = new Set(pendingRes.data.map(p => p.supplierCode));
        this.suppliers = this.allSuppliers.filter(s => pendingCodes.has(s.supplierCode));

        this.stops.controls.forEach((c, idx) => {
          const sid = c.get('supplierId')?.value;
          if (sid && this.stopUIStates[idx]) {
            const s = this.allSuppliers.find(x => x.id === sid);
            if (s) this.stopUIStates[idx].supplierName = `${s.supplierCode} - ${s.name}`;
          }
        });
      });
    });
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
    this.masterService.getRoutes('', 1, 1000).subscribe(res => {
      this.routes = res.data;
      this.filteredRoutes = [...this.routes];
      
      const currentRouteCode = this.form.get('routeCode')?.value;
      if (currentRouteCode) {
        const r = this.routes.find(x => x.routeCode === currentRouteCode);
        if (r) this.selectedRouteName = `${r.routeCode} (${r.cycleCode})`;
      }
    });
    this.masterService.getTruckAssignments('', 1, 1000).subscribe(res => {
      this.truckAssignments = res.data;
    });
  }

  // Supplier Dropdown Methods (Per Stop)
  getFilteredSuppliers(stopIndex: number) {
    const state = this.stopUIStates[stopIndex];
    if (!state || !state.supplierSearchText) return this.suppliers;
    const term = state.supplierSearchText.toLowerCase();
    return this.suppliers.filter(s => 
      s.supplierCode?.toLowerCase().includes(term) || 
      s.name?.toLowerCase().includes(term)
    );
  }

  onSupplierSearch(stopIndex: number, event: any) {
    if (this.stopUIStates[stopIndex]) {
      this.stopUIStates[stopIndex].supplierSearchText = event.target.value;
      this.stopUIStates[stopIndex].supplierName = event.target.value;
    }
  }

  onSupplierBlur(stopIndex: number) {
    setTimeout(() => {
      if (this.stopUIStates[stopIndex]) {
        this.stopUIStates[stopIndex].supplierDropdownOpen = false;
        
        // Reset name to selected supplier if they didn't select anything
        const control = this.stops.at(stopIndex).get('supplierId');
        if (control?.value) {
          const s = this.allSuppliers.find(x => x.id === control.value) || this.suppliers.find(x => x.id === control.value);
          if (s) {
            this.stopUIStates[stopIndex].supplierName = `${s.supplierCode} - ${s.name}`;
          }
        } else {
          this.stopUIStates[stopIndex].supplierName = '';
        }
        this.cdr.detectChanges();
      }
    }, 200);
  }

  selectSupplier(stopIndex: number, supplier: any) {
    if (this.stopUIStates[stopIndex]) {
      this.stops.at(stopIndex).get('supplierId')?.setValue(supplier?.id || null);
      if (supplier) {
        this.stopUIStates[stopIndex].supplierName = `${supplier.supplierCode} - ${supplier.name}`;
      } else {
        this.stopUIStates[stopIndex].supplierName = '';
      }
      this.stopUIStates[stopIndex].supplierDropdownOpen = false;
      this.stopUIStates[stopIndex].supplierSearchText = '';
    }
  }

  onDriverFocus() {
    this.driverDropdownOpen = true;
    this.filteredDrivers = this.getDriversByTruck();
  }

  onDriverSearch(event: Event) {
    this.driverDropdownOpen = true;
    const term = (event.target as HTMLInputElement).value.toLowerCase();
    const baseDrivers = this.getDriversByTruck();
    
    if (!term) {
      this.filteredDrivers = [...baseDrivers];
    } else {
      this.filteredDrivers = baseDrivers.filter(d => 
        d.name.toLowerCase().includes(term) || d.nik.toLowerCase().includes(term)
      );
    }
  }

  private getDriversByTruck(): Driver[] {
    const currentTruckId = this.form.get('truckId')?.value;
    if (currentTruckId) {
      const truck = this.trucks.find(t => t.id === currentTruckId);
      if (truck && truck.logisticPartnerId) {
        return this.drivers.filter(d => d.logisticPartnerId === truck.logisticPartnerId);
      }
    }
    return this.drivers;
  }

  onDriverBlur() {
    // Delay slightly so that mousedown on options can fire before the dropdown is removed
    setTimeout(() => {
      this.driverDropdownOpen = false;
      this.cdr.detectChanges();
    }, 200);
  }

  selectDriver(driver: Driver | null) {
    if (driver) {
      this.form.patchValue({ driverId: driver.id });
      this.selectedDriverName = `${driver.name} (${driver.nik})`;
      this.isDriverAutoFilled = false; // Manually selected

      // Auto-fill Truck
      const currentTruck = this.form.get('truckId')?.value;
      if (!currentTruck || this.isTruckAutoFilled) {
        const assignment = this.truckAssignments.find(a => a.driverId === driver.id);
        if (assignment) {
          const truck = this.trucks.find(t => t.id === assignment.truckId);
          if (truck) {
            this.form.patchValue({ truckId: truck.id });
            this.selectedTruckName = `${truck.plateNumber} (${truck.vehicleType || '-'})`;
            this.isTruckAutoFilled = true; // Mark as auto-filled
          }
        } else if (this.isTruckAutoFilled) {
          // If previous was auto-filled but new driver has no truck, clear it
          this.form.patchValue({ truckId: null });
          this.selectedTruckName = '';
          this.isTruckAutoFilled = false;
        }
      }
    } else {
      this.form.patchValue({ driverId: null });
      this.selectedDriverName = '';
      this.isDriverAutoFilled = false;
    }
    this.driverDropdownOpen = false;
  }

  onTruckFocus() {
    this.truckDropdownOpen = true;
    this.filteredTrucks = this.getTrucksByDriver();
  }

  onTruckSearch(event: Event) {
    this.truckDropdownOpen = true;
    const term = (event.target as HTMLInputElement).value.toLowerCase();
    const baseTrucks = this.getTrucksByDriver();

    if (!term) {
      this.filteredTrucks = [...baseTrucks];
    } else {
      this.filteredTrucks = baseTrucks.filter(t => 
        t.plateNumber.toLowerCase().includes(term) || (t.vehicleType && t.vehicleType.toLowerCase().includes(term))
      );
    }
  }

  private getTrucksByDriver(): Truck[] {
    const currentDriverId = this.form.get('driverId')?.value;
    if (currentDriverId) {
      const driver = this.drivers.find(d => d.id === currentDriverId);
      if (driver && driver.logisticPartnerId) {
        return this.trucks.filter(t => t.logisticPartnerId === driver.logisticPartnerId);
      }
    }
    return this.trucks;
  }

  onTruckBlur() {
    setTimeout(() => {
      this.truckDropdownOpen = false;
      this.cdr.detectChanges();
    }, 200);
  }

  selectTruck(truck: Truck | null) {
    if (truck) {
      this.form.patchValue({ truckId: truck.id });
      this.selectedTruckName = `${truck.plateNumber} (${truck.vehicleType || '-'})`;
      this.isTruckAutoFilled = false; // Manually selected

      // Auto-fill Driver
      const currentDriver = this.form.get('driverId')?.value;
      if (!currentDriver || this.isDriverAutoFilled) {
        const assignment = this.truckAssignments.find(a => a.truckId === truck.id);
        if (assignment) {
          const driver = this.drivers.find(d => d.id === assignment.driverId);
          if (driver) {
            this.form.patchValue({ driverId: driver.id });
            this.selectedDriverName = `${driver.name} (${driver.nik})`;
            this.isDriverAutoFilled = true; // Mark as auto-filled
          }
        } else if (this.isDriverAutoFilled) {
          // If previous was auto-filled but new truck has no driver, clear it
          this.form.patchValue({ driverId: null });
          this.selectedDriverName = '';
          this.isDriverAutoFilled = false;
        }
      }
    } else {
      this.form.patchValue({ truckId: null });
      this.selectedTruckName = '';
      this.isTruckAutoFilled = false;
    }
    this.truckDropdownOpen = false;
  }

  // Route Dropdown Methods
  onRouteSearch(event: Event) {
    this.routeDropdownOpen = true;
    const term = (event.target as HTMLInputElement).value.toLowerCase();
    if (!term) {
      this.filteredRoutes = [...this.routes];
    } else {
      this.filteredRoutes = this.routes.filter(r => 
        r.routeCode.toLowerCase().includes(term) || r.cycleCode.toLowerCase().includes(term)
      );
    }
  }

  onRouteBlur() {
    setTimeout(() => {
      this.routeDropdownOpen = false;
      this.cdr.detectChanges();
    }, 200);
  }

  selectRoute(route: Route | null) {
    if (route) {
      this.form.patchValue({ routeCode: route.routeCode, cycleCode: route.cycleCode });
      this.selectedRouteName = `${route.routeCode} (${route.cycleCode})`;
    } else {
      this.form.patchValue({ routeCode: '', cycleCode: '' });
      this.selectedRouteName = '';
    }
    this.routeDropdownOpen = false;
  }

  loadOrder(id: number) {
    this.loading = true;
    this.routeService.getPickupOrderById(id).subscribe({
      next: (res) => {
        const order = res.data;
        this.form.patchValue({

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
        
        // Try to set the selected route name if routes are already loaded
        if (order.routeCode && this.routes.length > 0) {
          const r = this.routes.find(x => x.routeCode === order.routeCode);
          if (r) this.selectedRouteName = `${r.routeCode} (${r.cycleCode})`;
        }

        // Load stops
        this.stops.clear();
        this.stopUIStates = [];
        order.details?.forEach(stop => {
          const stopGroup = this.fb.group({
            supplierId: [stop.supplierId, Validators.required],
            sequence: [stop.sequence],
            manifests: this.fb.array([])
          });
          const manifestsArray = stopGroup.get('manifests') as FormArray;
          
          this.routeService.getPickupOrderStopManifests(order.id, stop.id, 1, 1000).subscribe({
            next: (res) => {
              res.data?.forEach(m => {
                manifestsArray.push(this.fb.group({
                  manifestNo: [m.manifestNo, Validators.required],
                  totalKanban: [m.totalKanban, Validators.required],
                  totalPart: [m.totalPart || 0],
                  orderType: [m.orderType],
                  totalSkid: [m.totalSkid],
                  dockCode: [m.dockCode]
                }));
              });
              this.cdr.markForCheck();
            }
          });
          
          this.setupStopSupplierSubscription(stopGroup);
          
          this.stopUIStates.push({
            page: 1,
            pageSize: 5,
            search: '',
            selectedCheckboxes: new Set<number>(),
            isLoading: false
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
    
    // Auto-filter by supplier if selected
    const stop = this.stops.at(stopIndex) as FormGroup;
    const supplierId = stop.get('supplierId')?.value;
    let supplierCode = '';
    
    if (supplierId) {
      const supplier = this.allSuppliers.find(s => s.id === supplierId) || this.suppliers.find(s => s.id === supplierId);
      if (supplier) supplierCode = supplier.supplierCode;
    }

    this.manifestModalState = {
      search: '',
      page: 1,
      pageSize: 10,
      totalPages: 1,
      supplierCode: supplierCode
    };

    this.loadModalManifests();
  }

  loadModalManifests() {
    this.manifestLoading = true;
    this.cargoService.getManifests(
      this.manifestModalState.search,
      this.manifestModalState.supplierCode,
      'Pending',
      false,
      this.manifestModalState.page,
      this.manifestModalState.pageSize
    ).subscribe({
      next: (res) => {
        this.availableManifests = res.data;
        this.manifestModalState.totalPages = res.pagination?.total_pages || 1;
        this.manifestLoading = false;
      },
      error: () => {
        this.manifestLoading = false;
      }
    });
  }

  onManifestSearch(term: string) {
    this.manifestModalState.search = term;
    this.manifestModalState.page = 1;
    this.loadModalManifests();
  }

  onManifestPageChange(newPage: number) {
    if (newPage >= 1 && newPage <= this.manifestModalState.totalPages) {
      this.manifestModalState.page = newPage;
      this.loadModalManifests();
    }
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
        // Prevent duplicate manifest addition
        const exists = manifestsArray.value.some((extM: any) => extM.manifestNo === m.manifestNo);
        if (!exists) {
          manifestsArray.push(this.fb.group({
            manifestNo: [m.manifestNo, Validators.required],
            totalKanban: [m.totalKanbans, Validators.required],
            totalPart: [m.totalParts || 0],
            orderType: ['ORG'],
            totalSkid: [1],
            dockCode: ['-']
          }));
        }
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
      totalPart: [0],
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
