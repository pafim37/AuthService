import { Component, inject } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-header-component',
  imports: [MatButton],
  templateUrl: './app-header-component.html',
  styleUrl: './app-header-component.css',
})
export class AppHeaderComponent {
  private readonly authService = inject(AuthService);

  public readonly currentUser = this.authService.currentUser;

  public logout(): void {
    this.authService.logout();
  }
}
