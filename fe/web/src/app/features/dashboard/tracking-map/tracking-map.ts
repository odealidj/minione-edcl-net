import { Component, ElementRef, OnDestroy, OnInit, ViewChild, effect, inject, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import { SignalrService, TruckLocationUpdate } from '../../../core/services/signalr.service';
import { DashboardService } from '../../../core/services/dashboard.service';

// Fix leaflet default icon issue
const iconRetinaUrl = 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png';
const iconUrl = 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png';
const shadowUrl = 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png';
const DefaultIcon = L.icon({
  iconUrl,
  iconRetinaUrl,
  shadowUrl,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  tooltipAnchor: [16, -28],
  shadowSize: [41, 41]
});
L.Marker.prototype.options.icon = DefaultIcon;

@Component({
  selector: 'app-tracking-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tracking-map.html',
  styleUrls: ['./tracking-map.css'],
  encapsulation: ViewEncapsulation.None
})
export class TrackingMapComponent implements OnInit, OnDestroy {
  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef;

  private map!: L.Map;
  private markers: Map<number, L.Marker> = new Map();
  
  private signalrService = inject(SignalrService);
  private dashboardService = inject(DashboardService);

  // Custom truck icon
  private truckIcon = L.divIcon({
    html: '<div style="font-size: 28px; line-height: 1; text-shadow: 2px 2px 4px rgba(0,0,0,0.4); text-align: center;">🚚</div>',
    className: 'custom-truck-icon',
    iconSize: [32, 32],
    iconAnchor: [16, 16],
    popupAnchor: [0, -16],
    tooltipAnchor: [16, -16]
  });

  constructor() {
    // Listen to updates from SignalR Service
    effect(() => {
      const locations = this.signalrService.truckLocations();
      
      locations.forEach((location, truckId) => {
        const latLng: L.LatLngTuple = [location.latitude, location.longitude];
        
        const content = `
          <div class="text-sm font-sans p-1">
            <strong class="block text-indigo-700 mb-1 border-b pb-1">Truck ID: ${truckId}</strong>
            ${!location.isConnected ? '<span class="text-white bg-red-500 rounded px-1 py-0.5 text-xs font-bold mb-1 inline-block">Tidak terkoneksi dengan GPS Vendor</span>' : ''}
            <span class="text-gray-800 font-semibold block mt-1">Plate: ${location.plateNumber || '-'}</span>
            <span class="text-gray-800 font-semibold block mb-1">Delivery: ${location.deliveryNo || 'Tidak ada (Idle)'}</span>
            <span class="text-gray-600 block">Speed: ${location.speed.toFixed(1)} km/h</span>
            <span class="text-gray-500 block text-xs mt-1">Provider: ${location.providerName}</span>
          </div>
        `;

        if (this.markers.has(truckId)) {
          // Update existing marker
          const marker = this.markers.get(truckId)!;
          marker.setLatLng(latLng);
          
          marker.getPopup()?.setContent(content);
          marker.getTooltip()?.setContent(content);
        } else {
          // Create new marker
          const marker = L.marker(latLng, { icon: this.truckIcon }).addTo(this.map);
          marker.bindPopup(content);
          marker.bindTooltip(content, { direction: 'top', offset: [0, -16] });
          this.markers.set(truckId, marker);
          
          // Pan map to the first truck we see if it's the only one
          if (this.markers.size === 1) {
            this.map.setView(latLng, 13);
          }
        }
      });
    });
  }

  ngOnInit() {
    this.initMap();
    this.signalrService.startConnection();
    this.signalrService.addLocationListener();

    // Fetch initial latest locations
    this.dashboardService.getLiveFleets().subscribe(res => {
      if (res.status === 'success' && res.data) {
        const initialMap = new Map<number, TruckLocationUpdate>();
        res.data.forEach((loc: any) => {
          initialMap.set(loc.truckId, loc);
        });
        this.signalrService.truckLocations.set(initialMap);
      }
    });
  }

  ngOnDestroy() {
    this.signalrService.stopConnection();
    if (this.map) {
      this.map.remove();
    }
  }

  private initMap() {
    // Default center (e.g., Jakarta / TMMIN area)
    this.map = L.map(this.mapContainer.nativeElement).setView([-6.317423, 107.143744], 12);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>'
    }).addTo(this.map);
  }
}
