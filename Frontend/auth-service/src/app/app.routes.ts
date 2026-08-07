import { Routes } from '@angular/router';
import { AdminPanelComponent } from '../components/admin-panel-component/admin-panel-component';
import { LoginPageComponent } from '../components/login-page-component/login-page-component';
import { authGuard } from '../services/auth-guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },
  {
    component: LoginPageComponent,
    path: 'login',
  },
  {
    // TODO: impove that to protect resources not the UI
    canActivate: [authGuard],
    component: AdminPanelComponent,
    path: 'admin',
  },
];
