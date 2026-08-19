import { Routes } from '@angular/router';
import { authGuard } from '../services/auth-guard';
import { loginPageGuard } from '../services/login-page-guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'sign-in',
  },
  {
    canActivate: [loginPageGuard],
    loadComponent: () =>
      import('../components/login-page-component/login-page-component').then(
        (component) => component.LoginPageComponent,
      ),
    path: 'sign-in',
  },
  {
    canActivate: [authGuard],
    loadComponent: () =>
      import('../components/admin-panel-component/admin-panel-component').then(
        (component) => component.AdminPanelComponent,
      ),
    path: 'dashboard',
  },
];
