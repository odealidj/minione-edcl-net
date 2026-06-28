import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    // Load from local storage on init
    const token = localStorage.getItem('access_token');
    if (token) {
      this.currentUserSubject.next({ token }); // Simplified
    }
  }

  public get currentUserValue(): any {
    return this.currentUserSubject.value;
  }

  login(email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/admin/login`, { email, password })
      .pipe(
        tap(response => {
          if (response && response.data && response.data.accessToken) {
            localStorage.setItem('access_token', response.data.accessToken);
            if (response.data.refreshToken) {
              localStorage.setItem('refresh_token', response.data.refreshToken);
            }
            this.currentUserSubject.next({ token: response.data.accessToken });
          }
        })
      );
  }

  logout() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem('access_token');
  }
}
