import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';

interface SignInRequest {
  login: string;
  password: string;
}

export interface AuthToken {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
}

interface CurrentUser {
  login: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  readonly currentUser = signal<string | null>(null);

  constructor(private readonly httpClient: HttpClient) {}

  login(username: string, password: string): Observable<AuthToken> {
    const request: SignInRequest = {
      login: username,
      password,
    };

    return this.httpClient.post<AuthToken>('/api/auth/admin-sign-in', request, { withCredentials: true }).pipe(
      tap(() => {
        this.currentUser.set(username);
      }),
    );
  }

  loadCurrentUser(): Observable<string | null> {
    return this.httpClient.get<CurrentUser>('/api/auth/me', { withCredentials: true }).pipe(
      map((user) => user.login),
      tap((login) => this.currentUser.set(login)),
      catchError(() => {
        this.currentUser.set(null);
        return of(null); // TODO: verify if of is not deprecated 
      }),
    );
  }

  logout(): Observable<void> {
    return this.httpClient.post<void>('/api/auth/logout', {}, { withCredentials: true }).pipe(
      catchError(() => of(undefined)),
      tap(() => this.currentUser.set(null)),
    );
  }
}
