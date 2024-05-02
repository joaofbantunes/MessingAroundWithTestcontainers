using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Api.Tests;

// ReSharper disable once ClassNeverInstantiated.Global - automagically instantiated by xUnit
public class ValkeyFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("valkey/valkey:7.2-alpine")
        .WithPortBinding(6379, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
        .Build();

    public string Endpoint => $"{_container.Hostname}:{_container.GetMappedPublicPort(6379)}";
    
    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}