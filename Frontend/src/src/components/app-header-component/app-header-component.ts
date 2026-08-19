import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { Router } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { AuthService } from '../../services/auth-service';
import { Snackbar } from '../snackbar/snackbar';
import {
  ChangePasswordDialogComponent,
  ChangePasswordDialogResult,
} from '../dialogs/change-password-dialog-component/change-password-dialog-component';

type ThemeMode = 'light' | 'dark';

@Component({
  selector: 'app-header-component',
  imports: [MatButtonModule, MatDividerModule, MatIconModule, MatMenuModule, MatSlideToggle],
  templateUrl: './app-header-component.html',
  styleUrl: './app-header-component.css',
})
export class AppHeaderComponent {
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly snackBar = inject(Snackbar);

  public readonly currentUser = this.authService.currentUser;
  public readonly isDarkTheme = signal(this.getStoredTheme() === 'dark');

  constructor() {
    this.applyTheme(this.isDarkTheme() ? 'dark' : 'light');
  }

  public logout(): void {
    this.authService
      .logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.router.navigateByUrl('/sign-in');
      });
  }

  public changePassword(): void {
    this.dialog
      .open<ChangePasswordDialogComponent, unknown, ChangePasswordDialogResult>(ChangePasswordDialogComponent)
      .afterClosed()
      .pipe(
        filter((result): result is ChangePasswordDialogResult => !!result),
        switchMap((result) => this.authService.changePassword(result)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.snackBar.openSnackBar('Password changed successfully.'),
        error: (e) => {
          if (e.status === 401) {
            this.snackBar.openSnackBar('Current password is incorrect.');
          } else {
            this.snackBar.openSnackBar('Cannot change password.');
          }
        },
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
