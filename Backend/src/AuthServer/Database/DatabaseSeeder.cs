using AuthServer.Database.Models;
using AuthServer.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database
{
    internal static class DatabaseSeeder
    {
        internal static async Task SeedAsync(AuthContext authContext, IConfiguration configuration, CancellationToken cancellationToken = default)
        {
            const string adminLogin = "admin";
            const string administratorRoleName = "Administrator";
            const string fullPrivilegeName = "Full";

            PrivilegeEntity? fullPrivilege = await authContext.Privileges
                .FirstOrDefaultAsync(privilege => privilege.Name == fullPrivilegeName, cancellationToken)
                .ConfigureAwait(false);

            if (fullPrivilege is null)
            {
                fullPrivilege = new()
                {
                    Id = Guid.NewGuid(),
                    Name = fullPrivilegeName
                };

                await authContext.Privileges.AddAsync(fullPrivilege, cancellationToken).ConfigureAwait(false);
            }

            RoleEntity? administratorRole = await authContext.Roles
                .Include(role => role.Privileges)
                .FirstOrDefaultAsync(role => role.Name == administratorRoleName, cancellationToken)
                .ConfigureAwait(false);

            if (administratorRole is null)
            {
                administratorRole = new()
                {
                    Id = Guid.NewGuid(),
                    Name = administratorRoleName,
                    Privileges = [fullPrivilege]
                };

                await authContext.Roles.AddAsync(administratorRole, cancellationToken).ConfigureAwait(false);
            }
            else if (!administratorRole.Privileges.Any(privilege => privilege.Name == fullPrivilegeName))
            {
                administratorRole.Privileges.Add(fullPrivilege);
            }

            UserEntity? adminUser = await authContext.Users
                .FirstOrDefaultAsync(user => user.Login == adminLogin, cancellationToken)
                .ConfigureAwait(false);

            if (adminUser is null)
            {
                string adminPassword = configuration["SeedAdmin:Password"]
                    ?? Environment.GetEnvironmentVariable("AUTH_SERVICE_ADMIN_PASSWORD")
                    ?? throw new InvalidOperationException("Built-in admin user does not exist and seed password is not configured. Set SeedAdmin:Password or AUTH_SERVICE_ADMIN_PASSWORD to create the initial admin account.");

                adminUser = new()
                {
                    Id = Guid.NewGuid(),
                    Login = adminLogin,
                    PasswordHashed = PasswordHasher.HashPassword(adminPassword),
                    RoleId = administratorRole.Id,
                    Role = administratorRole
                };

                await authContext.Users.AddAsync(adminUser, cancellationToken).ConfigureAwait(false);
            }

            RoleEntity? defaultRoleEntity = await authContext.Roles
                .FirstOrDefaultAsync(role => role.Name == "Default", cancellationToken)
                .ConfigureAwait(false);

            if (defaultRoleEntity is null)
            {
                defaultRoleEntity = new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Default",
                    Privileges = []
                };

                await authContext.Roles.AddAsync(defaultRoleEntity, cancellationToken).ConfigureAwait(false);
            }

            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}


