import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface Privilege {
  id: string;
  name: string;
}

export interface Role {
  id: string;
  name: string;
  privileges: Privilege[];
}

export interface User {
  id: string;
  login: string;
  role: Role | null;
}

export interface UserRequest {
  login: string;
  password: string;
  role: string;
}

export interface RoleRequest {
  name: string;
  privileges: string[];
}

export interface PrivilegeRequest {
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdminApiService {
  private readonly httpClient = inject(HttpClient);

  getUsers() {
    return this.httpClient.get<User[]>('/api/users');
  }

  createUser(request: UserRequest) {
    return this.httpClient.post<User>('/api/users', request);
  }

  createAdmin(login: string, password: string) {
    return this.httpClient.post<User>('/api/users/create-admin', { login, password });
  }

  updateUser(id: string, request: UserRequest) {
    return this.httpClient.put<User>(`/api/users/${id}`, request);
  }

  deleteUser(id: string) {
    return this.httpClient.delete<void>(`/api/users/${id}`);
  }

  getRoles() {
    return this.httpClient.get<Role[]>('/api/roles');
  }

  createRole(request: RoleRequest) {
    return this.httpClient.post<Role>('/api/roles', request);
  }

  updateRole(id: string, request: RoleRequest) {
    return this.httpClient.put<Role>(`/api/roles/${id}`, request);
  }

  deleteRole(id: string) {
    return this.httpClient.delete<void>(`/api/roles/${id}`);
  }

  getPrivileges() {
    return this.httpClient.get<Privilege[]>('/api/privileges');
  }

  createPrivilege(request: PrivilegeRequest) {
    return this.httpClient.post<Privilege>('/api/privileges', request);
  }

  updatePrivilege(id: string, request: PrivilegeRequest) {
    return this.httpClient.put<Privilege>(`/api/privileges/${id}`, request);
  }

  deletePrivilege(id: string) {
    return this.httpClient.delete<void>(`/api/privileges/${id}`);
  }
}
