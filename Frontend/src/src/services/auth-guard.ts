import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth-service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // TODO: impove that to protect resources not the UI
  return authService.currentUser() ? true : router.parseUrl('/login');
};
