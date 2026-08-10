namespace AuthServerIntegrationTests.Unauthorized;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<DockerComposeFixture>
{
    public const string Name = "Integration endpoint tests";
}
