import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Driver as DriverModel, MasterDataService } from '../../../core/services/master-data.service';

@Component({
  selector: 'app-driver',
  imports: [CommonModule],
  templateUrl: './driver.html',
  styleUrl: './driver.css'
})
export class Driver implements OnInit {
  drivers: DriverModel[] = [];
  isLoading = true;

  private masterDataService = inject(MasterDataService);

  ngOnInit(): void {
    this.masterDataService.getDrivers().subscribe({
      next: (data) => {
        this.drivers = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load drivers', err);
        this.isLoading = false;
      }
    });
  }
}
