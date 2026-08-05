import { Component, ElementRef, OnDestroy, OnInit, ViewChild, effect, inject, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import { SignalrService } from '../../../core/services/signalr.service';

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

  // Custom truck icon
  private truckIcon = L.icon({
    iconUrl: 'https://cdn-icons-png.flaticon.com/512/3209/3209995.png', // A simple truck icon
    iconSize: [32, 32],
    iconAnchor: [16, 16],
    popupAnchor: [0, -16]
  });

  constructor() {
    // Listen to updates from SignalR Service
    effect(() => {
      const locations = this.signalrService.truckLocations();
      
      locations.forEach((location, truckId) => {
        const latLng: L.LatLngTuple = [location.latitude, location.longitude];
        
        if (this.markers.has(truckId)) {
          // Update existing marker
          const marker = this.markers.get(truckId)!;
          marker.setLatLng(latLng);
          
          // Optionally update popup content
          const popupContent = `
            <div class="text-sm font-sans">
              <strong class="block text-indigo-700 mb-1">Truck ID: ${truckId}</strong>
              <span class="text-gray-600 block">Speed: ${location.speed.toFixed(1)} km/h</span>
              <span class="text-gray-600 block text-xs mt-1">Provider: ${location.providerName}</span>
            </div>
          `;
          marker.getPopup()?.setContent(popupContent);
        } else {
          // Create new marker
          const marker = L.marker(latLng, { icon: this.truckIcon }).addTo(this.map);
          marker.bindPopup(`
            <div class="text-sm font-sans">
              <strong class="block text-indigo-700 mb-1">Truck ID: ${truckId}</strong>
              <span class="text-gray-600 block">Speed: ${location.speed.toFixed(1)} km/h</span>
              <span class="text-gray-600 block text-xs mt-1">Provider: ${location.providerName}</span>
            </div>
          `);
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
