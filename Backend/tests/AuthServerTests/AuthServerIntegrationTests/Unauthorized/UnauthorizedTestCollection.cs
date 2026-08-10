namespace AuthServerIntegrationTests.Unauthorized;

[CollectionDefinition(Name)]
public sealed class UnauthorizedTestCollection : ICollectionFixture<DockerComposeFixture>
{
    public const string Name = "Unauthorized endpoint tests";
}
