using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
namespace AuthServerIntegrationTests;

public sealed class DockerComposeFixture : IAsyncLifetime
{
    private const string ComposeProjectName = "auth-service-integration-tests";
    private const string DockerFilename = "docker-compose.backend.integration.yml";
    private const string DatabaseContainerName = $"{ComposeProjectName}-database-1";
    private static readonly TimeSpan ApiStartupTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DatabaseStartupTimeout = TimeSpan.FromMinutes(2);
    private static readonly Uri ApiBaseAddress = new("http://localhost:5124");

    private readonly string repoRoot = FindRepoRoot();
    private readonly string composeFilePath;

    public DockerComposeFixture()
    {
        composeFilePath = Path.Combine(repoRoot, ".docker", DockerFilename);
        Client = new HttpClient(new HttpClientHandler
        {
            UseCookies = false
        })
        {
            BaseAddress = ApiBaseAddress,
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public HttpClient Client { get; }

    public async Task InitializeAsync()
    {
        if (TestConfig.RunInDocker)
        {
            await RunDockerComposeAsync("down", "--volumes", "--remove-orphans");
            await RunDockerComposeAsync("up", "-d", "--build", "database", "--force-recreate");
            await WaitForDatabaseAsync();
            await RunDockerComposeAsync("up", "-d", "--build", "backend", "--force-recreate");
        }
        await WaitForApiAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (TestConfig.RunInDocker)
        {
            await RunDockerComposeAsync("down", "--volumes", "--remove-orphans");
        }
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

    private static async Task WaitForDatabaseAsync()
    {
        using CancellationTokenSource timeout = new(DatabaseStartupTimeout);

        while (!timeout.IsCancellationRequested)
        {
            string status = await RunDockerAsync("inspect", DatabaseContainerName, "--format", "{{.State.Health.Status}}")
                .ConfigureAwait(false);

            if (status.Trim().Equals("healthy", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token).ConfigureAwait(false);
        }

        string logs = await RunDockerAsync("logs", DatabaseContainerName).ConfigureAwait(false);
        throw new TimeoutException($"Database did not become healthy within {DatabaseStartupTimeout}.{Environment.NewLine}{logs}");
    }

    private async Task RunDockerComposeAsync(params string[] arguments)
    {
        string dockerArguments = $"compose -p {ComposeProjectName} -f \"{composeFilePath}\" {string.Join(' ', arguments)}";
        await RunProcessAsync("docker", dockerArguments).ConfigureAwait(false);
    }

    private static async Task<string> RunDockerAsync(params string[] arguments)
    {
        return await RunProcessAsync("docker", string.Join(' ', arguments)).ConfigureAwait(false);
    }

    private static async Task<string> RunProcessAsync(string fileName, string arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
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
                $"{fileName} {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}{standardOutput}{standardError}");
        }

        return standardOutput;
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".docker", DockerFilename)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find repository root containing .docker/{DockerFilename}.");
    }
}
