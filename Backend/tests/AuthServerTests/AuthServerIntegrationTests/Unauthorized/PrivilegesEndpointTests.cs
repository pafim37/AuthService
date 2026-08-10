using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AuthServerIntegrationTests.Unauthorized;

[Collection(IntegrationTestCollection.Name)]
public sealed class PrivilegesEndpointTests(DockerComposeFixture fixture)
{
    [Fact]
    public async Task GetPrivileges_WithoutAccessToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await fixture.Client.GetAsync("/api/privileges");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPrivilege_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        using HttpResponseMessage response = await fixture.Client.GetAsync($"/api/privileges/{guid}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrivilege_WithoutAccessToken_ReturnsUnauthorized()
    {
        var userData = new { Name = "privilegeName", Description = "privilegeDescription" };
        HttpContent body = JsonContent.Create(userData);
        using HttpResponseMessage response = await fixture.Client.PostAsync("/api/privileges", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePrivilege_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        var userData = new { Name = "privilegeName", Description = "privilegeDescription" };
        HttpContent body = JsonContent.Create(userData);
        using HttpResponseMessage response = await fixture.Client.PutAsync($"/api/privileges/{guid}", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchPrivilege_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        var userData = new { Name = "privilegeName", Description = "privilegeDescription" };
        HttpContent body = JsonContent.Create(userData);
        using HttpResponseMessage response = await fixture.Client.PatchAsync($"/api/privileges/{guid}", body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeletePrivilege_WithoutAccessToken_ReturnsUnauthorized()
    {
        Guid guid = Guid.NewGuid();
        using HttpResponseMessage response = await fixture.Client.DeleteAsync($"/api/privileges/{guid}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
