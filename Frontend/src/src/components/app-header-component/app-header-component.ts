import { Component, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth-service';

type ThemeMode = 'light' | 'dark';

@Component({
  selector: 'app-header-component',
  imports: [MatButton, MatSlideToggle],
  templateUrl: './app-header-component.html',
  styleUrl: './app-header-component.css',
})
export class AppHeaderComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  public readonly currentUser = this.authService.currentUser;
  public readonly isDarkTheme = signal(this.getStoredTheme() === 'dark');

  constructor() {
    this.applyTheme(this.isDarkTheme() ? 'dark' : 'light');
  }

  public logout(): void {
    this.authService.logout().subscribe(() => {
      this.router.navigateByUrl('/sign-in');
    });
  }

  public toggleTheme(event: MatSlideToggleChange): void {
    const theme = event.checked ? 'dark' : 'light';

    this.isDarkTheme.set(event.checked);
    this.applyTheme(theme);
    localStorage.setItem('theme', theme);
  }

  private getStoredTheme(): ThemeMode {
    return localStorage.getItem('theme') === 'dark' ? 'dark' : 'light';
  }

  private applyTheme(theme: ThemeMode): void {
    document.body.classList.toggle('dark-theme', theme === 'dark');
    document.body.classList.toggle('light-theme', theme === 'light');
  }
}
