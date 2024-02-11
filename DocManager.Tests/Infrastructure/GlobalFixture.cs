// Ignore Spelling: Antivirus

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

using Xunit.Extensions.AssemblyFixture;

[assembly: TestFramework(AssemblyFixtureFramework.TypeName, AssemblyFixtureFramework.AssemblyName)]

namespace DocManager.Tests.Infrastructure;

public class GlobalFixture : IAsyncLifetime
{
    private const int CLAMAV_PORT = 3310;

    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("clamav/clamav:latest")
        .WithExposedPort(CLAMAV_PORT)
        .WithPortBinding(CLAMAV_PORT, true)
        .WithVolumeMount("clamavData", "/var/lib/clamav", AccessMode.ReadWrite)
        .WithWaitStrategy(Wait
            .ForUnixContainer()
            .UntilPortIsAvailable(CLAMAV_PORT)
            .UntilMessageIsLogged("clamd started"))
        .Build();

    public int GetPort() => _container.GetMappedPublicPort(CLAMAV_PORT);

    public string GetUrl() => _container.Hostname;

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    async Task IAsyncLifetime.DisposeAsync() => await _container.StopAsync().ConfigureAwait(false);
}
