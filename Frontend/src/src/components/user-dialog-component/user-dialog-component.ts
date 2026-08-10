import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Role, User } from '../../services/admin-api-service';

export type UserDialogData = {
  mode: 'create' | 'create-admin' | 'edit';
  roles: Role[];
  user?: User;
};

@Component({
  selector: 'app-user-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './user-dialog-component.html',
  styleUrl: './user-dialog-component.css',
})
export class UserDialogComponent {
  protected readonly data = inject<UserDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<UserDialogComponent>);
  protected readonly title =
    this.data.mode === 'create-admin'
      ? 'New admin'
      : this.data.mode === 'edit'
        ? 'Edit user'
        : 'New user';

  protected readonly form = new FormGroup({
    login: new FormControl(this.data.user?.login ?? '', {
      nonNullable: true,
      validators: Validators.required,
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: Validators.required,
    }),
    role: new FormControl(this.data.user?.role?.name ?? this.data.roles[0]?.name ?? 'administrator', {
      nonNullable: true,
      validators: Validators.required,
    }),
  });

  save(): void {
    if (this.form.invalid) {
      return;
    }

    const request = this.form.getRawValue();
    this.dialogRef.close({
      login: request.login,
      password: request.password,
      role: this.data.mode === 'create-admin' ? 'administrator' : request.role,
    });
  }
}
