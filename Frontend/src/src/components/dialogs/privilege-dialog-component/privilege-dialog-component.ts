import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Privilege } from '../../../services/admin-api-service';

export type PrivilegeDialogData = {
  privilege?: Privilege;
};

@Component({
  selector: 'app-privilege-dialog',
  imports: [MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, ReactiveFormsModule],
  templateUrl: './privilege-dialog-component.html',
  styleUrl: './privilege-dialog-component.css',
})
export class PrivilegeDialogComponent {
  protected readonly data = inject<PrivilegeDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<PrivilegeDialogComponent>);

  protected readonly form = new FormGroup({
    name: new FormControl(this.data.privilege?.name ?? '', {
      nonNullable: true,
      validators: Validators.required,
    }),
    description: new FormControl(this.data.privilege?.description ?? ''),
  });

  save(): void {
    if (this.form.invalid) {
      return;
    }

    this.dialogRef.close(this.form.getRawValue());
  }
}
