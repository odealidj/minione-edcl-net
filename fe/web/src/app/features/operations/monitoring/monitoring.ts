import { Component } from '@angular/core';

@Component({
  selector: 'app-monitoring',
  imports: [],
  templateUrl: './monitoring.html',
  styleUrl: './monitoring.css'
})
export class Monitoring {
  deliveries = [
    { no: 1, routeId: 'R-001', driver: 'BUDI SANTOSO', lp: 'LP-JABAR', status: 'In Transit', progress: 65, supplier: 'PT. MAKMUR SENTOSA', startTime: '08:00', estTime: '14:30' },
    { no: 2, routeId: 'R-002', driver: 'AGUS SUPRIYADI', lp: 'LP-JATENG', status: 'Delivered', progress: 100, supplier: 'CV. JAYA ABADI', startTime: '06:00', estTime: '11:00' },
    { no: 3, routeId: 'R-003', driver: 'JOKO WIDODO', lp: 'LP-JATIM', status: 'Loading', progress: 15, supplier: 'PT. MAKMUR SENTOSA', startTime: '10:00', estTime: '18:00' },
  ];
}
