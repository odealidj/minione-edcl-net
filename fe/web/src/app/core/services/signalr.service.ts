import { Injectable, signal, WritableSignal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

export interface TruckLocationUpdate {
  truckId: number;
  latitude: number;
  longitude: number;
  speed: number;
  heading: number;
  timestamp: string;
  providerName: string;
  isConnected: boolean;
  plateNumber?: string;
  deliveryNo?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | undefined;
  
  // WritableSignal to hold the latest locations mapped by truckId
  public truckLocations = signal<Map<number, TruckLocationUpdate>>(new Map());

  constructor() { }

  public startConnection = () => {
    const baseUrl = environment.apiUrl.replace('/api/v1/web', '');
    this.hubConnection = new signalR.HubConnectionBuilder()
      // Use the gateway or API URL
      .withUrl(`${baseUrl}/hubs/tracking`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Connection started'))
      .catch(err => console.log('Error while starting SignalR connection: ' + err));
  }

  public addLocationListener = () => {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveLocation', (data: TruckLocationUpdate) => {
      // Create a new map to trigger signal update
      const newMap = new Map(this.truckLocations());
      const existing = newMap.get(data.truckId);
      if (existing) {
        data.plateNumber = existing.plateNumber;
        data.deliveryNo = existing.deliveryNo;
      }
      newMap.set(data.truckId, data);
      this.truckLocations.set(newMap);
    });
  }
  
  public stopConnection = () => {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }
}
