using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AuthServerIntegrationTests.Unauthorized;

[Collection(IntegrationTestCollection.Name)]
public sealed class RolesEndpointTests(DockerComposeFixture fixture)
{
    [Fact]
    public async Task GetRoles_WithoutAccessToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await fixture.Client.GetAsync("/api/roles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRole_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        using HttpResponseMessage response = await fixture.Client.GetAsync($"/api/roles/{guid}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_WithoutAccessToken_ReturnsUnauthorized()
    {
        var roleData = new { Name = "roleName", Privileges = Array.Empty<string>() };
        HttpContent body = JsonContent.Create(roleData);
        using HttpResponseMessage response = await fixture.Client.PostAsync("/api/roles", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        var roleData = new { Name = "roleName", Privileges = Array.Empty<string>() };
        HttpContent body = JsonContent.Create(roleData);
        using HttpResponseMessage response = await fixture.Client.PutAsync($"/api/roles/{guid}", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchRole_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        var roleData = new { Name = "roleName", Privileges = Array.Empty<string>() };
        HttpContent body = JsonContent.Create(roleData);
        using HttpResponseMessage response = await fixture.Client.PatchAsync($"/api/roles/{guid}", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        using HttpResponseMessage response = await fixture.Client.DeleteAsync($"/api/roles/{guid}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
