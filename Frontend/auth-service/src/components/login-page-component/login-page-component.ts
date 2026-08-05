import { Component, inject } from '@angular/core';
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
import { AuthService } from '../../services/auth-service';
import { Snackbar } from '../incorrect-credentials-snackbar/incorrect-credentials-snackbar';

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
    ReactiveFormsModule,
  ],
  templateUrl: './login-page-component.html',
  styleUrl: './login-page-component.css',
})
export class LoginPageComponent {
  private readonly _authService = inject(AuthService);
  private readonly _snackBar = inject(Snackbar);

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

    const { username, password } = this.loginForm.getRawValue();
    const isLoggedIn : Boolean = this._authService.login(username, password);

    if (!isLoggedIn) {
      this.showIncorrectCredentials();
    }
  }

  private showIncorrectCredentials(): void {
    this._snackBar.openSnackBar('Incorrect credential');
  }
}
