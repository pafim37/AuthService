import { Routes } from '@angular/router';
import { LoginPageComponent } from '../components/login-page-component/login-page-component';

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
];
