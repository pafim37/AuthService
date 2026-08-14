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
import { Snackbar } from '../snackbar/snackbar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

type LoginFormGroup = FormGroup<{
  login: FormControl<string>;
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

  readonly loginForm: LoginFormGroup = new FormGroup({
    login: new FormControl('', {
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
      this._snackBar.openSnackBar('Login and password are required!');
      return;
    }

    const { login, password } = this.loginForm.getRawValue();
    this.loginForm.disabled;

    this._authService
      .signin(login, password)
      .subscribe({
        next: () => {
          this._router.navigateByUrl('/dashboard');
        },
        error: (e) => {
          if (e.status !== 403) {
            this._snackBar.openSnackBar('Incorrect credential!');
          }
          else {
            this._snackBar.openSnackBar('No sufficient permission!');
          }
        },
      });
  }
}
