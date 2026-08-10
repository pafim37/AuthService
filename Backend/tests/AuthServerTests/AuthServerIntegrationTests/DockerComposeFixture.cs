using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AuthServerIntegrationTests;

public sealed class DockerComposeFixture : IAsyncLifetime
{
    private const string ComposeProjectName = "auth-service-integration-tests";
    private static readonly TimeSpan ApiStartupTimeout = TimeSpan.FromMinutes(5);
    private static readonly Uri ApiBaseAddress = new("http://localhost:5124");

    private readonly string repoRoot = FindRepoRoot();
    private readonly string composeFilePath;

    public DockerComposeFixture()
    {
        composeFilePath = Path.Combine(repoRoot, ".docker", "docker-compose.yml");
    }

    public HttpClient Client { get; private set; } = new()
    {
        BaseAddress = ApiBaseAddress,
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task InitializeAsync()
    {
        await RunDockerComposeAsync("down", "--volumes", "--remove-orphans");
        await RunDockerComposeAsync("up", "-d", "--build", "backend");
        await WaitForApiAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await RunDockerComposeAsync("down", "--volumes", "--remove-orphans");
    }

    private async Task WaitForApiAsync()
    {
        using CancellationTokenSource timeout = new(ApiStartupTimeout);

        while (!timeout.IsCancellationRequested)
        {
            try
            {
                using HttpResponseMessage response = await Client.GetAsync("/api/privileges", timeout.Token);
                return;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
            }
        }

        throw new TimeoutException($"API did not start at {Client.BaseAddress} within {ApiStartupTimeout}.");
    }

    private async Task RunDockerComposeAsync(params string[] arguments)
    {
        string dockerArguments = $"compose -p {ComposeProjectName} -f \"{composeFilePath}\" {string.Join(' ', arguments)}";

        ProcessStartInfo startInfo = new()
        {
            FileName = "docker",
            Arguments = dockerArguments,
            WorkingDirectory = repoRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start docker compose.");

        string standardOutput = await process.StandardOutput.ReadToEndAsync();
        string standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker {dockerArguments} failed with exit code {process.ExitCode}.{Environment.NewLine}{standardOutput}{standardError}");
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".docker", "docker-compose.yml")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing .docker/docker-compose.yml.");
    }
}
