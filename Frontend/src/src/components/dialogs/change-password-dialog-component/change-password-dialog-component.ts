import { Component, inject } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ChangePasswordDialogResult {
  newPassword: string;
}

@Component({
  selector: 'app-change-password-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './change-password-dialog-component.html',
  styleUrl: './change-password-dialog-component.css',
})
export class ChangePasswordDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ChangePasswordDialogComponent>);

  protected readonly form = new FormGroup({
    newPassword: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
  }, { validators: this.passwordsMatch });

  protected save(): void {
    if (this.form.invalid) {
      return;
    }

    this.dialogRef.close({ newPassword: this.form.controls.newPassword.value });
  }

  private passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }
}
