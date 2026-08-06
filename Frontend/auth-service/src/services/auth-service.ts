import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  readonly currentUser = signal<string | null>(null);

  login(username: string, password: string): boolean {
    if (username === 'admin' && password === 'admin') {
      this.currentUser.set(username);
      return true;
    }

    return false;
  }

  logout(): void {
    this.currentUser.set(null);
  }
}
