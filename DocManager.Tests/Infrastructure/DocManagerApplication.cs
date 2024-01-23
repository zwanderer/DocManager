using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using MongoDB.Driver;

using Testcontainers.MongoDb;

namespace DocManager.Tests.Infrastructure;

public class DocManagerApplication : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongodb/mongodb-community-server:7.0-ubi8")
        .WithAutoRemove(true)
        .WithUsername("admin")
        .WithPassword("password")
        .Build();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("IsDocManagerTests", "true");
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
    }

    public async Task InitializeAsync() => await _container.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _container.StopAsync();
}
