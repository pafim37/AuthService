import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Privilege, Role } from '../../services/admin-api-service';

export type RoleDialogData = {
  privileges: Privilege[];
  role?: Role;
};

@Component({
  selector: 'app-role-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './role-dialog-component.html',
  styleUrl: './role-dialog-component.css',
})
export class RoleDialogComponent {
  protected readonly data = inject<RoleDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<RoleDialogComponent>);

  protected readonly form = new FormGroup({
    name: new FormControl(this.data.role?.name ?? '', {
      nonNullable: true,
      validators: Validators.required,
    }),
    privileges: new FormControl(this.data.role?.privileges.map((privilege) => privilege.name) ?? [], {
      nonNullable: true,
    }),
  });

  save(): void {
    if (this.form.invalid) {
      return;
    }

    this.dialogRef.close(this.form.getRawValue());
  }
}
