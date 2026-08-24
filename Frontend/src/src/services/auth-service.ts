import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';

interface SignInRequest {
  login: string;
  password: string;
}

export interface AuthSession {
  expiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
}

interface CurrentUser {
  login: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

@Injectable({
  providedIn: 'root',
})

export class AuthService {
  readonly currentUser = signal<string | null>(null);

  constructor(private readonly httpClient: HttpClient) {}

  signin(login: string, password: string): Observable<AuthSession> {
    const request: SignInRequest = {
      login,
      password,
    };

    return this.httpClient.post<AuthSession>('/api/auth/admin-sign-in', request).pipe(
      tap(() => {
        this.currentUser.set(login);
      }),
    );
  }

  loadCurrentUser(): Observable<string | null> {
    return this.httpClient.get<CurrentUser>('/api/auth/me').pipe(
      map((user) => user.login),
      tap((login) => this.currentUser.set(login)),
      catchError(() => {
        this.currentUser.set(null);
        return of(null); 
      }),
    );
  }

  logout(): Observable<void> {
    return this.httpClient.post<void>('/api/auth/logout', {}).pipe(
      catchError(() => of(undefined)),
      tap(() => this.currentUser.set(null)),
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.httpClient.post<void>('/api/auth/change-password', request);
  }
}
