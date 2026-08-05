import { Component } from '@angular/core';
import { MatFormField, MatLabel } from '@angular/material/form-field';

@Component({
  selector: 'app-login-page-component',
  imports: [MatLabel, MatFormField],
  templateUrl: './login-page-component.html',
  styleUrl: './login-page-component.css',
})
export class LoginPageComponent {}
