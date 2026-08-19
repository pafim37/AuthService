using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AuthServerIntegrationTests.Unauthorized;

namespace AuthServerIntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class EndpointReturnTests(DockerComposeFixture fixture)
{
    private static readonly Guid MissingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task AuthSignUp_ReturnsBadRequest_WhenPayloadIsInvalid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-up", new { Login = "", Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthSignUp_ReturnsConflict_WhenLoginAlreadyExists()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-up", new { Login = "admin", Password = "admin" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AuthSignUp_ReturnsCreated_WhenPayloadIsValid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-up", new { Login = Unique("signup"), Password = "password" });

        AuthTokenDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(tokens?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens?.RefreshToken));
    }

    [Fact]
    public async Task AuthSignIn_ReturnsBadRequest_WhenPayloadIsInvalid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-in", new { Login = "", Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthSignIn_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-in", new { Login = Unique("missing"), Password = "password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthSignIn_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-in", new { Login = "admin", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthSignIn_ReturnsOk_WhenCredentialsAreValid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-in", new { Login = "admin", Password = "admin" });

        AuthTokenDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(tokens?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens?.RefreshToken));
    }

    [Fact]
    public async Task AuthRefresh_ReturnsBadRequest_WhenRefreshTokenIsMissing()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthRefresh_ReturnsUnauthorized_WhenRefreshTokenIsInvalid()
    {
        using HttpResponseMessage response = await SendWithRefreshTokenCookieAsync("/api/auth/refresh", "invalid-refresh-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthRefresh_ReturnsOk_WhenRefreshTokenIsValid()
    {
        AuthTokenDto tokens = await SignInAsync("admin", "admin");

        using HttpResponseMessage response = await SendWithRefreshTokenCookieAsync("/api/auth/refresh", tokens.RefreshToken);

        AuthTokenDto? refreshedTokens = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(refreshedTokens?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshedTokens?.RefreshToken));
    }

    [Fact]
    public async Task AuthAdminSignIn_ReturnsBadRequest_WhenPayloadIsInvalid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/admin-sign-in", new { Login = "", Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthAdminSignIn_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/admin-sign-in", new { Login = Unique("missing"), Password = "password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthAdminSignIn_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/admin-sign-in", new { Login = "admin", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthAdminSignIn_ReturnsForbidden_WhenUserIsNotAdministrator()
    {
        string login = Unique("non-admin-user");

        await fixture.Client.PostAsJsonAsync("/api/auth/sign-up", new { Login = login, Password = "password" });

        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/admin-sign-in", new { Login = login, Password = "password" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthAdminSignIn_ReturnsOk_WhenAdministratorCredentialsAreValid()
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/admin-sign-in", new { Login = "admin", Password = "admin" });

        AuthTokenDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(tokens?.AccessToken));
    }

    [Fact]
    public async Task AuthLogout_ReturnsUnauthorized_WhenAccessTokenIsMissing()
    {
        ClearAuthorization();

        using HttpResponseMessage response = await SendWithRefreshTokenCookieAsync("/api/auth/logout", "anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthLogout_ReturnsBadRequest_WhenRefreshTokenIsMissing()
    {
        await AuthorizeAsAdminAsync();

        using HttpResponseMessage response = await fixture.Client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthLogout_ReturnsOk_WhenRefreshTokenIsUnknown()
    {
        await AuthorizeAsAdminAsync();

        using HttpResponseMessage response = await SendWithRefreshTokenCookieAsync("/api/auth/logout", "unknown-refresh-token");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthLogout_ReturnsOk_WhenRefreshTokenIsActive()
    {
        AuthTokenDto tokens = await SignInAsync("admin", "admin");
        SetAuthorization(tokens.AccessToken);

        using HttpResponseMessage response = await SendWithRefreshTokenCookieAsync("/api/auth/logout", tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthChangePassword_ChangesPasswordForAuthenticatedUser()
    {
        string login = Unique("password-user");
        string originalPassword = "password";
        string newPassword = "new-password";

        using HttpResponseMessage signUpResponse = await fixture.Client.PostAsJsonAsync("/api/auth/sign-up", new { Login = login, Password = originalPassword });
        AuthTokenDto tokens = (await signUpResponse.Content.ReadFromJsonAsync<AuthTokenDto>())!;
        SetAuthorization(tokens.AccessToken);

        using HttpResponseMessage changePasswordResponse = await fixture.Client.PostAsJsonAsync("/api/auth/change-password", new { CurrentPassword = originalPassword, NewPassword = newPassword });
        Assert.Equal(HttpStatusCode.OK, changePasswordResponse.StatusCode);

        using HttpResponseMessage oldPasswordResponse = await fixture.Client.PostAsJsonAsync("/api/auth/sign-in", new { Login = login, Password = originalPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordResponse.StatusCode);

        using HttpResponseMessage newPasswordResponse = await fixture.Client.PostAsJsonAsync("/api/auth/sign-in", new { Login = login, Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newPasswordResponse.StatusCode);
    }

    [Fact]
    public async Task PrivilegesEndpoints_ReturnAllReturnPaths()
    {
        await AuthorizeAsAdminAsync();
        string privilegeName = Unique("privilege");
        string updatedPrivilegeName = Unique("privilege-updated");

        using HttpResponseMessage listResponse = await fixture.Client.GetAsync("/api/privileges");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using HttpResponseMessage missingGetResponse = await fixture.Client.GetAsync($"/api/privileges/{MissingId}");
        Assert.Equal(HttpStatusCode.NotFound, missingGetResponse.StatusCode);

        using HttpResponseMessage invalidCreateResponse = await fixture.Client.PostAsJsonAsync("/api/privileges", new { Name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCreateResponse.StatusCode);

        PrivilegeDto privilege = await CreatePrivilegeAsync(privilegeName);

        using HttpResponseMessage duplicateCreateResponse = await fixture.Client.PostAsJsonAsync("/api/privileges", new { Name = privilegeName });
        Assert.Equal(HttpStatusCode.Conflict, duplicateCreateResponse.StatusCode);

        using HttpResponseMessage getResponse = await fixture.Client.GetAsync($"/api/privileges/{privilege.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using HttpResponseMessage invalidUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/privileges/{privilege.Id}", new { Name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdateResponse.StatusCode);

        using HttpResponseMessage missingUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/privileges/{MissingId}", new { Name = Unique("missing-update") });
        Assert.Equal(HttpStatusCode.NotFound, missingUpdateResponse.StatusCode);

        using HttpResponseMessage conflictUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/privileges/{privilege.Id}", new { Name = "Full" });
        Assert.Equal(HttpStatusCode.Conflict, conflictUpdateResponse.StatusCode);

        using HttpResponseMessage updateResponse = await fixture.Client.PutAsJsonAsync($"/api/privileges/{privilege.Id}", new { Name = updatedPrivilegeName });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using HttpResponseMessage missingPatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/privileges/{MissingId}", new { Name = Unique("missing-patch") });
        Assert.Equal(HttpStatusCode.NotFound, missingPatchResponse.StatusCode);

        using HttpResponseMessage conflictPatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/privileges/{privilege.Id}", new { Name = "Full" });
        Assert.Equal(HttpStatusCode.Conflict, conflictPatchResponse.StatusCode);

        using HttpResponseMessage patchResponse = await fixture.Client.PatchAsJsonAsync($"/api/privileges/{privilege.Id}", new { Description = "patched description" });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        using HttpResponseMessage missingDeleteResponse = await fixture.Client.DeleteAsync($"/api/privileges/{MissingId}");
        Assert.Equal(HttpStatusCode.NotFound, missingDeleteResponse.StatusCode);

        PrivilegeDto fullPrivilege = await GetPrivilegeByNameAsync("Full");
        using HttpResponseMessage protectedDeleteResponse = await fixture.Client.DeleteAsync($"/api/privileges/{fullPrivilege.Id}");
        Assert.Equal(HttpStatusCode.Conflict, protectedDeleteResponse.StatusCode);

        using HttpResponseMessage deleteResponse = await fixture.Client.DeleteAsync($"/api/privileges/{privilege.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task RolesEndpoints_ReturnAllReturnPaths()
    {
        await AuthorizeAsAdminAsync();
        string privilegeName = Unique("role-privilege");
        string roleName = Unique("role");
        string updatedRoleName = Unique("role-updated");

        await CreatePrivilegeAsync(privilegeName);

        using HttpResponseMessage listResponse = await fixture.Client.GetAsync("/api/roles");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using HttpResponseMessage missingGetResponse = await fixture.Client.GetAsync($"/api/roles/{MissingId}");
        Assert.Equal(HttpStatusCode.NotFound, missingGetResponse.StatusCode);

        using HttpResponseMessage invalidCreateResponse = await fixture.Client.PostAsJsonAsync("/api/roles", new { Name = "", Privileges = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCreateResponse.StatusCode);

        RoleDto role = await CreateRoleAsync(roleName, privilegeName);

        using HttpResponseMessage duplicateCreateResponse = await fixture.Client.PostAsJsonAsync("/api/roles", new { Name = roleName, Privileges = new[] { privilegeName } });
        Assert.Equal(HttpStatusCode.Conflict, duplicateCreateResponse.StatusCode);

        using HttpResponseMessage missingPrivilegeCreateResponse = await fixture.Client.PostAsJsonAsync("/api/roles", new { Name = Unique("role-missing-privilege"), Privileges = new[] { Unique("missing-privilege") } });
        Assert.Equal(HttpStatusCode.BadRequest, missingPrivilegeCreateResponse.StatusCode);

        using HttpResponseMessage getResponse = await fixture.Client.GetAsync($"/api/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using HttpResponseMessage invalidUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/roles/{role.Id}", new { Name = "", Privileges = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdateResponse.StatusCode);

        using HttpResponseMessage missingUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/roles/{MissingId}", new { Name = Unique("missing-update"), Privileges = new[] { privilegeName } });
        Assert.Equal(HttpStatusCode.NotFound, missingUpdateResponse.StatusCode);

        using HttpResponseMessage conflictUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/roles/{role.Id}", new { Name = "administrator", Privileges = new[] { privilegeName } });
        Assert.Equal(HttpStatusCode.Conflict, conflictUpdateResponse.StatusCode);

        using HttpResponseMessage missingPrivilegeUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/roles/{role.Id}", new { Name = updatedRoleName, Privileges = new[] { Unique("missing-privilege") } });
        Assert.Equal(HttpStatusCode.BadRequest, missingPrivilegeUpdateResponse.StatusCode);

        using HttpResponseMessage updateResponse = await fixture.Client.PutAsJsonAsync($"/api/roles/{role.Id}", new { Name = updatedRoleName, Privileges = new[] { privilegeName } });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using HttpResponseMessage missingPatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/roles/{MissingId}", new { Name = Unique("missing-patch") });
        Assert.Equal(HttpStatusCode.NotFound, missingPatchResponse.StatusCode);

        using HttpResponseMessage conflictPatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/roles/{role.Id}", new { Name = "administrator" });
        Assert.Equal(HttpStatusCode.Conflict, conflictPatchResponse.StatusCode);

        using HttpResponseMessage missingPrivilegePatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/roles/{role.Id}", new { Privileges = new[] { Unique("missing-privilege") } });
        Assert.Equal(HttpStatusCode.BadRequest, missingPrivilegePatchResponse.StatusCode);

        using HttpResponseMessage patchResponse = await fixture.Client.PatchAsJsonAsync($"/api/roles/{role.Id}", new { Name = Unique("role-patched"), Privileges = new[] { privilegeName } });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        using HttpResponseMessage missingDeleteResponse = await fixture.Client.DeleteAsync($"/api/roles/{MissingId}");
        Assert.Equal(HttpStatusCode.NotFound, missingDeleteResponse.StatusCode);

        RoleDto administratorRole = await GetRoleByNameAsync("Administrator");
        using HttpResponseMessage protectedDeleteResponse = await fixture.Client.DeleteAsync($"/api/roles/{administratorRole.Id}");
        Assert.Equal(HttpStatusCode.Conflict, protectedDeleteResponse.StatusCode);

        using HttpResponseMessage deleteResponse = await fixture.Client.DeleteAsync($"/api/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UsersEndpoints_ReturnAllReturnPaths()
    {
        await AuthorizeAsAdminAsync();
        string userLogin = Unique("user");
        string otherUserLogin = Unique("user-other");
        string updatedUserLogin = Unique("user-updated");

        using HttpResponseMessage listResponse = await fixture.Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using HttpResponseMessage missingGetResponse = await fixture.Client.GetAsync($"/api/users/{MissingId}");
        Assert.Equal(HttpStatusCode.NotFound, missingGetResponse.StatusCode);

        using HttpResponseMessage invalidCreateAdminResponse = await fixture.Client.PostAsJsonAsync("/api/users/create-admin", new { Login = "", Password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCreateAdminResponse.StatusCode);

        using HttpResponseMessage createAdminResponse = await fixture.Client.PostAsJsonAsync("/api/users/create-admin", new { Login = Unique("created-admin"), Password = "password" });
        Assert.Equal(HttpStatusCode.Created, createAdminResponse.StatusCode);

        using HttpResponseMessage invalidCreateResponse = await fixture.Client.PostAsJsonAsync("/api/users", new { Login = "", Password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCreateResponse.StatusCode);

        UserDto user = await CreateUserAsync(userLogin);
        UserDto otherUser = await CreateUserAsync(otherUserLogin);

        using HttpResponseMessage duplicateCreateResponse = await fixture.Client.PostAsJsonAsync("/api/users", new { Login = userLogin, Password = "password" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateCreateResponse.StatusCode);

        using HttpResponseMessage getResponse = await fixture.Client.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using HttpResponseMessage invalidUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/users/{user.Id}", new { Login = "", Password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdateResponse.StatusCode);

        using HttpResponseMessage missingUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/users/{MissingId}", new { Login = Unique("missing-update"), Password = "password" });
        Assert.Equal(HttpStatusCode.NotFound, missingUpdateResponse.StatusCode);

        using HttpResponseMessage conflictUpdateResponse = await fixture.Client.PutAsJsonAsync($"/api/users/{user.Id}", new { Login = otherUser.Login, Password = "password" });
        Assert.Equal(HttpStatusCode.Conflict, conflictUpdateResponse.StatusCode);

        using HttpResponseMessage updateResponse = await fixture.Client.PutAsJsonAsync($"/api/users/{user.Id}", new { Login = updatedUserLogin, Password = "password" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using HttpResponseMessage missingPatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/users/{MissingId}", new { Login = Unique("missing-patch") });
        Assert.Equal(HttpStatusCode.NotFound, missingPatchResponse.StatusCode);

        using HttpResponseMessage conflictPatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/users/{user.Id}", new { Login = otherUser.Login });
        Assert.Equal(HttpStatusCode.Conflict, conflictPatchResponse.StatusCode);

        using HttpResponseMessage missingRolePatchResponse = await fixture.Client.PatchAsJsonAsync($"/api/users/{user.Id}", new { Role = Unique("missing-role") });
        Assert.Equal(HttpStatusCode.BadRequest, missingRolePatchResponse.StatusCode);

        using HttpResponseMessage patchResponse = await fixture.Client.PatchAsJsonAsync($"/api/users/{user.Id}", new { Login = Unique("user-patched"), Password = "new-password" });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        using HttpResponseMessage missingDeleteResponse = await fixture.Client.DeleteAsync($"/api/users/{MissingId}");
        Assert.Equal(HttpStatusCode.NotFound, missingDeleteResponse.StatusCode);

        UserDto adminUser = await GetUserByLoginAsync("admin");
        using HttpResponseMessage protectedDeleteResponse = await fixture.Client.DeleteAsync($"/api/users/{adminUser.Id}");
        Assert.Equal(HttpStatusCode.Conflict, protectedDeleteResponse.StatusCode);

        using HttpResponseMessage deleteResponse = await fixture.Client.DeleteAsync($"/api/users/{otherUser.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private async Task AuthorizeAsAdminAsync()
    {
        AuthTokenDto tokens = await SignInAsync("admin", "admin");
        SetAuthorization(tokens.AccessToken);
    }

    private async Task<HttpResponseMessage> SendWithRefreshTokenCookieAsync(string url, string refreshToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, url);
        request.Headers.Add("Cookie", $"auth_refresh_token={refreshToken}");
        return await fixture.Client.SendAsync(request);
    }

    private async Task<AuthTokenDto> SignInAsync(string login, string password)
    {
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/sign-in", new { Login = login, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    private void SetAuthorization(string accessToken)
    {
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private void ClearAuthorization()
    {
        fixture.Client.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<PrivilegeDto> CreatePrivilegeAsync(string name)
    {
        await AuthorizeAsAdminAsync();
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/privileges", new { Name = name, Description = "description" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PrivilegeDto>())!;
    }

    private async Task<RoleDto> CreateRoleAsync(string name, params string[] privileges)
    {
        await AuthorizeAsAdminAsync();
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/roles", new { Name = name, Privileges = privileges });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoleDto>())!;
    }

    private async Task<UserDto> CreateUserAsync(string login)
    {
        await AuthorizeAsAdminAsync();
        using HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/users", new { Login = login, Password = "password" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    private async Task<PrivilegeDto> GetPrivilegeByNameAsync(string name)
    {
        IEnumerable<PrivilegeDto> privileges = await fixture.Client.GetFromJsonAsync<IEnumerable<PrivilegeDto>>("/api/privileges") ?? [];
        return privileges.Single(privilege => privilege.Name == name);
    }

    private async Task<RoleDto> GetRoleByNameAsync(string name)
    {
        IEnumerable<RoleDto> roles = await fixture.Client.GetFromJsonAsync<IEnumerable<RoleDto>>("/api/roles") ?? [];
        return roles.Single(role => role.Name == name);
    }

    private async Task<UserDto> GetUserByLoginAsync(string login)
    {
        IEnumerable<UserDto> users = await fixture.Client.GetFromJsonAsync<IEnumerable<UserDto>>("/api/users") ?? [];
        return users.Single(user => user.Login == login);
    }

    private static string Unique(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private sealed class AuthTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class PrivilegeDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class RoleDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class UserDto
    {
        public Guid Id { get; set; }
        public string? Login { get; set; }
    }
}
