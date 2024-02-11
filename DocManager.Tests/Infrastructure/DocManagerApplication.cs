using MartinCostello.Logging.XUnit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MongoDB.Driver;

using Testcontainers.MongoDb;

using Xunit.Abstractions;
using Xunit.Extensions.AssemblyFixture;

namespace DocManager.Tests.Infrastructure;

public class DocManagerApplication(GlobalFixture globalFixture) :
    WebApplicationFactory<Program>,
    IAsyncLifetime,
    IAssemblyFixture<GlobalFixture>,
    ITestOutputHelperAccessor
{
    private readonly GlobalFixture _globalFixture = globalFixture;

    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongodb/mongodb-community-server:7.0-ubi8")
        .WithAutoRemove(true)
        .WithUsername("admin")
        .WithPassword("password")
        .Build();

    public ITestOutputHelper? OutputHelper { get; set; }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("IsDocManagerTests", "true");
        Environment.SetEnvironmentVariable("ClamAV__URL", _globalFixture.GetUrl());
        Environment.SetEnvironmentVariable("ClamAV__Port", _globalFixture.GetPort().ToString());
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IMongoClient));
            services.AddSingleton<IMongoClient>(new MongoClient(_container.GetConnectionString()));
        });

        builder.ConfigureLogging(builder => builder.AddXUnit(this));
    }

    public async Task InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    async Task IAsyncLifetime.DisposeAsync() => await _container.StopAsync().ConfigureAwait(false);
}
