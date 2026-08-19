namespace AuthServerIntegrationTests
{
    internal static class TestConfig
    {
        public static readonly bool RunInDocker = !bool.TryParse(
            System.Environment.GetEnvironmentVariable("AUTH_SERVICE_RUN_IN_DOCKER"),
            out bool runInDocker) || runInDocker;
    }
}
