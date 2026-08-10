using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AuthServerIntegrationTests.Unauthorized;

[Collection(UnauthorizedTestCollection.Name)]
public sealed class UsersEndpointTests(DockerComposeFixture fixture)
{
    [Fact]
    public async Task GetUsers_WithoutAccessToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await fixture.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        using HttpResponseMessage response = await fixture.Client.GetAsync($"/api/users/{guid}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_WithoutAccessToken_ReturnsUnauthorized()
    {
        var userData = new { Login = "adminLogin", Password = "adminPassword" };
        HttpContent body = JsonContent.Create(userData);
        using HttpResponseMessage response = await fixture.Client.PostAsync("/api/users/create-admin", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithoutAccessToken_ReturnsUnauthorized()
    {
        var userData = new { Login = "userLogin", Password = "userPassword", Role = "administrator" };
        HttpContent body = JsonContent.Create(userData);
        using HttpResponseMessage response = await fixture.Client.PostAsync("/api/users", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        var userData = new { Login = "userLogin", Password = "userPassword", Role = "administrator" };
        HttpContent body = JsonContent.Create(userData);
        using HttpResponseMessage response = await fixture.Client.PutAsync($"/api/users/{guid}", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchUser_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        var userData = new { Login = "userLogin", Password = "userPassword", Role = "administrator" };
        HttpContent body = JsonContent.Create(userData);
        using HttpResponseMessage response = await fixture.Client.PatchAsync($"/api/users/{guid}", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        using HttpResponseMessage response = await fixture.Client.DeleteAsync($"/api/users/{guid}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
