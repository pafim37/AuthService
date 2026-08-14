import { Routes } from '@angular/router';
import { AdminPanelComponent } from '../components/admin-panel-component/admin-panel-component';
import { LoginPageComponent } from '../components/login-page-component/login-page-component';
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
    component: LoginPageComponent,
    path: 'sign-in',
  },
  {
    canActivate: [authGuard],
    component: AdminPanelComponent,
    path: 'panel',
  },
];
