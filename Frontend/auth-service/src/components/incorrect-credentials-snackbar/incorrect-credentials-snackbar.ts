import { Component, Injectable, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import {
  MAT_SNACK_BAR_DATA,
  MatSnackBar,
  MatSnackBarHorizontalPosition,
  MatSnackBarRef,
  MatSnackBarVerticalPosition,
} from '@angular/material/snack-bar';

@Component({
  selector: 'app-incorrect-credentials-snackbar',
  imports: [MatButton],
  templateUrl: './incorrect-credentials-snackbar.html',
  styleUrl: './incorrect-credentials-snackbar.css',
})
export class SnackbarComponent {
  readonly message = inject<string>(MAT_SNACK_BAR_DATA);
  private readonly snackBarRef = inject(MatSnackBarRef<SnackbarComponent>);

  close(): void {
    this.snackBarRef.dismiss();
  }
}

@Injectable({
  providedIn: 'root',
})

export class Snackbar {
  private readonly snackBar = inject(MatSnackBar);

  readonly horizontalPosition = signal<MatSnackBarHorizontalPosition>('center');
  readonly verticalPosition = signal<MatSnackBarVerticalPosition>('top');

  openSnackBar(message: string): void {
    this.snackBar.openFromComponent(SnackbarComponent, {
      data: message,
      duration: 3000,
      horizontalPosition: this.horizontalPosition(),
      verticalPosition: this.verticalPosition(),
    });
  }
}
