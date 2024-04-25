using Testcontainers.LocalStack;

namespace Api.Tests;

// ReSharper disable once ClassNeverInstantiated.Global - automagically instantiated by xUnit
public class LocalStackFixture : IAsyncLifetime
{
    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder()
        .WithImage("localstack/localstack:3.3")
        .Build();
    
    public string ConnectionString => _localStackContainer.GetConnectionString();
    
    public async Task InitializeAsync() => await _localStackContainer.StartAsync();

    public async Task DisposeAsync() => await _localStackContainer.DisposeAsync();
}