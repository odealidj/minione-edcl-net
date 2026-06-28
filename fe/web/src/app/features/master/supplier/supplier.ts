import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Supplier as SupplierModel, MasterDataService } from '../../../core/services/master-data.service';

@Component({
  selector: 'app-supplier',
  imports: [CommonModule],
  templateUrl: './supplier.html',
  styleUrl: './supplier.css'
})
export class Supplier implements OnInit {
  suppliers: SupplierModel[] = [];
  isLoading = true;

  private masterDataService = inject(MasterDataService);

  ngOnInit(): void {
    this.masterDataService.getSuppliers().subscribe({
      next: (data) => {
        this.suppliers = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load suppliers', err);
        this.isLoading = false;
      }
    });
  }
}
