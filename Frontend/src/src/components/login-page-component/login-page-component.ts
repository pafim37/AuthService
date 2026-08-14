import { Component, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import {
  MatCard,
  MatCardActions,
  MatCardContent,
  MatCardHeader,
  MatCardSubtitle,
  MatCardTitle,
} from '@angular/material/card';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth-service';
import { Snackbar } from '../incorrect-credentials-snackbar/incorrect-credentials-snackbar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

type LoginFormGroup = FormGroup<{
  username: FormControl<string>;
  password: FormControl<string>;
}>;

@Component({
  selector: 'app-login-page-component',
  imports: [
    MatButton,
    MatCard,
    MatCardActions,
    MatCardContent,
    MatCardHeader,
    MatCardSubtitle,
    MatCardTitle,
    MatFormField,
    MatInput,
    MatLabel,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  templateUrl: './login-page-component.html',
  styleUrl: './login-page-component.css',
})
export class LoginPageComponent {
  private readonly _authService = inject(AuthService);
  private readonly _router = inject(Router);
  private readonly _snackBar = inject(Snackbar);
  protected readonly isSigningIn = signal(false);
  readonly isLoadingAdminPanel = signal(false);

  constructor() {
    effect(() => {
      if (!this._authService.currentUser()) {
        this.isSigningIn.set(false);
      }
    });
  }

  readonly loginForm: LoginFormGroup = new FormGroup({
    username: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
  });

  signIn(): void {
    if (this.loginForm.invalid) {
      this.showIncorrectCredentials();
      return;
    }

    if (this.isSigningIn()) {
      return;
    }

    const { username, password } = this.loginForm.getRawValue();
    this.isSigningIn.set(true);
    this.loginForm.reset();

    this._authService
      .login(username, password)
      .subscribe({
        next: () => {
          this.isLoadingAdminPanel.set(true),
          this._router.navigateByUrl('/admin')
        },
        error: () => {
          this.isLoadingAdminPanel.set(false);
          this.isSigningIn.set(false);
          this.showIncorrectCredentials();
        },
      });
  }

  private showIncorrectCredentials(): void {
    this._snackBar.openSnackBar('Incorrect credential');
  }
}
