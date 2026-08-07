import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

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

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  readonly currentUser = signal<string | null>(null);
  readonly authToken = signal<AuthToken | null>(null);

  constructor(private readonly httpClient: HttpClient) {}

  login(username: string, password: string): Observable<AuthToken> {
    const request: SignInRequest = {
      login: username,
      password,
    };

    return this.httpClient.post<AuthToken>('/api/auth/admin-sign-in', request).pipe(
      tap((authToken) => {
        this.authToken.set(authToken);
        this.currentUser.set(username);
      }),
    );
  }

  logout(): void {
    this.authToken.set(null);
    this.currentUser.set(null);
  }

  get accessToken(): string | null {
    return this.authToken()?.accessToken ?? null;
  }
}
