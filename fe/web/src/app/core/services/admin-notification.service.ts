import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';

export interface NotificationLogDto {
  id: number;
  driverId: number;
  title: string;
  message: string;
  type: string;
  pickupOrderId: number | null;
  isRead: boolean;
  fcmDeliveryStatus: string | null;
  fcmErrorMessage: string | null;
  createdAtUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminNotificationService {
  private apiUrl = `${environment.apiUrl}/v1/admin/notifications`;

  constructor(private http: HttpClient) {}

  getNotificationLogs(page: number = 1, pageSize: number = 10, driverId?: number, deliveryStatus?: string): Observable<ApiResponse<NotificationLogDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (driverId) {
      params = params.set('driverId', driverId.toString());
    }
    
    if (deliveryStatus) {
      params = params.set('deliveryStatus', deliveryStatus);
    }

    return this.http.get<ApiResponse<NotificationLogDto[]>>(`${this.apiUrl}/logs`, { params });
  }
}
