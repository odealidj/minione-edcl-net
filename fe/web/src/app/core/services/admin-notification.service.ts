import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.model';

export interface NotificationLogDto {
  id: number;
  driverId: number;
  driverName: string;
  title: string;
  message: string;
  type: string;
  pickupOrderId: number | null;
  poNo: string | null;
  routeCode: string | null;
  cycleCode: string | null;
  pickupDate: string | null;
  isRead: boolean;
  fcmDeliveryStatus: string | null;
  fcmErrorMessage: string | null;
  adminAlertAcknowledged: boolean;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminNotificationService {
  private apiUrl = `${environment.apiUrl}/admin/notifications`;

  constructor(private http: HttpClient) {}

  getNotificationLogs(page: number = 1, pageSize: number = 10, driverId?: number, deliveryStatus?: string, q?: string, isRead?: boolean | string): Observable<ApiResponse<NotificationLogDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (driverId) {
      params = params.set('driverId', driverId.toString());
    }
    
    if (deliveryStatus) {
      params = params.set('deliveryStatus', deliveryStatus);
    }

    if (q) {
      params = params.set('q', q);
    }

    if (isRead !== undefined && isRead !== null && isRead !== '') {
      params = params.set('isRead', isRead.toString());
    }

    return this.http.get<ApiResponse<NotificationLogDto[]>>(`${this.apiUrl}/logs`, { params });
  }

  getUnreadAlerts(): Observable<ApiResponse<NotificationLogDto[]>> {
    return this.http.get<ApiResponse<NotificationLogDto[]>>(`${this.apiUrl}/alerts`);
  }

  acknowledgeAlert(id: number): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.apiUrl}/alerts/${id}/acknowledge`, {});
  }

  resendNotification(id: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.apiUrl}/${id}/resend`, {});
  }
}
