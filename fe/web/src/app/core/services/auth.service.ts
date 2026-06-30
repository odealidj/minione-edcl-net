import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, finalize, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    // Load from local storage or session storage on init
    const token = this.getToken();
    if (token) {
      this.currentUserSubject.next(this.decodeToken(token));
    }
  }

  public get currentUserValue(): any {
    return this.currentUserSubject.value;
  }

  private decodeToken(token: string): any {
    try {
      const decoded: any = jwtDecode(token);
      return {
        token,
        id: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
        role: decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      };
    } catch (e) {
      return null;
    }
  }

  login(email: string, password: string, rememberMe: boolean = false): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/users/login`, { email, password })
      .pipe(
        tap(response => {
          if (response && response.data && response.data.accessToken) {
            const storage = rememberMe ? localStorage : sessionStorage;
            
            storage.setItem('access_token', response.data.accessToken);
            if (response.data.refreshToken) {
              storage.setItem('refresh_token', response.data.refreshToken);
            }
            this.currentUserSubject.next(this.decodeToken(response.data.accessToken));
          }
        })
      );
  }

  register(name: string, email: string, password: string, roleCode: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/users/register`, { name, email, password, roleCode });
  }

  refreshToken(): Observable<any> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<any>(`${this.apiUrl}/users/refresh-token`, { rawRefreshToken: refreshToken })
      .pipe(
        tap(response => {
          if (response && response.data && response.data.accessToken) {
            const storage = localStorage.getItem('refresh_token') ? localStorage : sessionStorage;
            
            storage.setItem('access_token', response.data.accessToken);
            if (response.data.refreshToken) {
              storage.setItem('refresh_token', response.data.refreshToken);
            }
            this.currentUserSubject.next(this.decodeToken(response.data.accessToken));
          }
        })
      );
  }

  getUsers(page: number = 1, pageSize: number = 10): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/users?page=${page}&pageSize=${pageSize}`);
  }

  updateUserRole(userId: number, roleCode: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/users/${userId}/role`, { userId, roleCode });
  }

  logout() {
    const refreshToken = localStorage.getItem('refresh_token') || sessionStorage.getItem('refresh_token');
    
    const finalizeLogout = () => {
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
      sessionStorage.removeItem('access_token');
      sessionStorage.removeItem('refresh_token');
      this.currentUserSubject.next(null);
    };

    if (refreshToken) {
      // Best effort to revoke the token on the server
      this.http.post(`${this.apiUrl}/users/logout`, { refreshToken })
        .pipe(finalize(() => finalizeLogout()))
        .subscribe({ error: () => {} }); // Ignore errors if server is down
    } else {
      finalizeLogout();
    }
  }

  getToken(): string | null {
    return localStorage.getItem('access_token') || sessionStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token') || sessionStorage.getItem('refresh_token');
  }
}
