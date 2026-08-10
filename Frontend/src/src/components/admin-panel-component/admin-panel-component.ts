import { AfterViewInit, Component, ViewChild, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { forkJoin } from 'rxjs';
import {
  AdminApiService,
  Privilege,
  PrivilegeRequest,
  Role,
  RoleRequest,
  User,
  UserRequest,
} from '../../services/admin-api-service';
import { ConfirmDialogComponent } from '../confirm-dialog-component/confirm-dialog-component';
import { Snackbar } from '../incorrect-credentials-snackbar/incorrect-credentials-snackbar';
import { PrivilegeDialogComponent, PrivilegeDialogData } from '../privilege-dialog-component/privilege-dialog-component';
import { RoleDialogComponent, RoleDialogData } from '../role-dialog-component/role-dialog-component';
import { UserDialogComponent, UserDialogData } from '../user-dialog-component/user-dialog-component';

@Component({
  selector: 'app-admin-panel-component',
  imports: [
    MatButtonModule,
    MatChipsModule,
    MatDialogModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatTableModule,
  ],
  templateUrl: './admin-panel-component.html',
  styleUrl: './admin-panel-component.css',
})
export class AdminPanelComponent implements AfterViewInit {
  private readonly adminApiService = inject(AdminApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(Snackbar);

  readonly userColumns = ['login', 'role', 'privileges', 'edit', 'remove'];
  readonly roleColumns = ['name', 'privileges', 'edit', 'remove'];
  readonly privilegeColumns = ['name', 'description', 'edit', 'remove'];

  readonly usersSource = new MatTableDataSource<User>([]);
  readonly rolesSource = new MatTableDataSource<Role>([]);
  readonly privilegesSource = new MatTableDataSource<Privilege>([]);
  readonly isLoading = signal(false);

  @ViewChild('usersPaginator') usersPaginator?: MatPaginator;
  @ViewChild('rolesPaginator') rolesPaginator?: MatPaginator;
  @ViewChild('privilegesPaginator') privilegesPaginator?: MatPaginator;

  ngAfterViewInit(): void {
    this.usersSource.paginator = this.usersPaginator ?? null;
    this.rolesSource.paginator = this.rolesPaginator ?? null;
    this.privilegesSource.paginator = this.privilegesPaginator ?? null;
    this.loadData();
  }

  openCreateUser(): void {
    this.openUserDialog({ mode: 'create', roles: this.rolesSource.data });
  }

  openCreateAdmin(): void {
    this.openUserDialog({ mode: 'create-admin', roles: this.rolesSource.data });
  }

  openEditUser(user: User): void {
    this.openUserDialog({ mode: 'edit', roles: this.rolesSource.data, user });
  }

  openCreateRole(): void {
    this.openRoleDialog({ privileges: this.privilegesSource.data });
  }

  openEditRole(role: Role): void {
    this.openRoleDialog({ privileges: this.privilegesSource.data, role });
  }

  openCreatePrivilege(): void {
    this.openPrivilegeDialog({});
  }

  openEditPrivilege(privilege: Privilege): void {
    this.openPrivilegeDialog({ privilege });
  }

  deleteUser(user: User): void {
    this.confirm(`Remove user "${user.login}"?`, () => this.adminApiService.deleteUser(user.id));
  }

  deleteRole(role: Role): void {
    this.confirm(`Remove role "${role.name}"?`, () => this.adminApiService.deleteRole(role.id));
  }

  deletePrivilege(privilege: Privilege): void {
    this.confirm(`Remove privilege "${privilege.name}"?`, () =>
      this.adminApiService.deletePrivilege(privilege.id),
    );
  }

  private loadData(): void {
    this.isLoading.set(true);

    forkJoin({
      privileges: this.adminApiService.getPrivileges(),
      roles: this.adminApiService.getRoles(),
      users: this.adminApiService.getUsers(),
    }).subscribe({
      next: ({ privileges, roles, users }) => {
        this.privilegesSource.data = privileges;
        this.rolesSource.data = roles;
        this.usersSource.data = users;
      },
      error: () => this.snackBar.openSnackBar('Cannot load administration data'),
      complete: () => this.isLoading.set(false),
    });
  }

  private openUserDialog(data: UserDialogData): void {
    this.dialog
      .open(UserDialogComponent, { data, width: '440px' })
      .afterClosed()
      .subscribe((request?: UserRequest) => {
        if (!request) {
          return;
        }

        const action =
          data.mode === 'create-admin'
            ? this.adminApiService.createAdmin(request.login, request.password)
            : data.mode === 'edit' && data.user
              ? this.adminApiService.updateUser(data.user.id, request)
              : this.adminApiService.createUser(request);

        action.subscribe({
          next: () => this.loadData(),
          error: () => this.snackBar.openSnackBar('Cannot save user'),
        });
      });
  }

  private openRoleDialog(data: RoleDialogData): void {
    this.dialog
      .open(RoleDialogComponent, { data, width: '440px' })
      .afterClosed()
      .subscribe((request?: RoleRequest) => {
        if (!request) {
          return;
        }

        const action = data.role
          ? this.adminApiService.updateRole(data.role.id, request)
          : this.adminApiService.createRole(request);

        action.subscribe({
          next: () => this.loadData(),
          error: () => this.snackBar.openSnackBar('Cannot save role'),
        });
      });
  }

  private openPrivilegeDialog(data: PrivilegeDialogData): void {
    this.dialog
      .open(PrivilegeDialogComponent, { data, width: '360px' })
      .afterClosed()
      .subscribe((request?: PrivilegeRequest) => {
        if (!request) {
          return;
        }

        const action = data.privilege
          ? this.adminApiService.updatePrivilege(data.privilege.id, request)
          : this.adminApiService.createPrivilege(request);

        action.subscribe({
          next: () => this.loadData(),
          error: () => this.snackBar.openSnackBar('Cannot save privilege'),
        });
      });
  }

  private confirm(message: string, actionFactory: () => ReturnType<AdminApiService['deleteUser']>): void {
    this.dialog
      .open(ConfirmDialogComponent, { data: message, width: '360px' })
      .afterClosed()
      .subscribe((confirmed?: boolean) => {
        if (!confirmed) {
          return;
        }

        actionFactory().subscribe({
          next: () => this.loadData(),
          error: () => this.snackBar.openSnackBar('Cannot remove item'),
        });
      });
  }
}
