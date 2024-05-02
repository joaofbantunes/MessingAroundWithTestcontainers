using System.Text;
using Amazon.SQS;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using static Api.Tests.RetryExtensions;
using static Api.SampleKeyGenerator;

namespace Api.Tests;

public class TestingRandomStuff(LocalStackFixture localStackFixture, ValkeyFixture valkeyFixture)
    : IClassFixture<LocalStackFixture>, IClassFixture<ValkeyFixture>, IAsyncLifetime
{
    private readonly string _sampleSqsQueueName = $"test-queue-{Guid.NewGuid()}";
    private IAmazonSQS _sqsClient = null!; // initialized in InitializeAsync
    private IConnectionMultiplexer _valkeyConnection = null!; // initialized in InitializeAsync

    [Fact]
    public async Task WhenDoingRandomTestsRandomStuffHappens()
    {
        await using var factory = WebApplicationFactoryBuilder.Create()
            .WithAwsSqs(localStackFixture.ConnectionString)
            .WithConfiguration("SqsQueueName", _sampleSqsQueueName)
            .WithConfiguration("ValkeyEndpoint", valkeyFixture.Endpoint)
            .Build();

        /*
         * As we're trying to test background services, and the host seems to be lazily initialized,
         * we need to do some explicit interaction with the factory, be it something like this, or creating an HttpClient,
         * to get things going.
         *
         * (yes, we could just instantiate the background service and test it directly, but was feeling like getting everything running instead)
         */
        _ = factory.Services.GetRequiredService<IConfiguration>();

        const string messageBody = "Hello LocalStack and Valkey in Testcontainers!";
        await _sqsClient.SendMessageAsync(
            _sampleSqsQueueName,
            messageBody);

        var value = await GetValkeyValue(GenerateKey(messageBody));
        value.Should().Be(messageBody);
    }

    private async Task<string> GetValkeyValue(string key)
    {
        var value = await ExecuteAndRetryAsync(
            () => _valkeyConnection.GetDatabase().StringGetAsync(key),
            v => v.HasValue,
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromSeconds(5)
        );
        return value!;
    }

    public async Task InitializeAsync()
    {
        _sqsClient = new AmazonSQSClient(new AmazonSQSConfig
        {
            ServiceURL = localStackFixture.ConnectionString
        });
        // this needs to run before the host starts up, otherwise it'll fail due to the queue not existing
        await _sqsClient.CreateQueueAsync(_sampleSqsQueueName);

        _valkeyConnection = await ConnectionMultiplexer.ConnectAsync(valkeyFixture.Endpoint);
    }

    public async Task DisposeAsync()
    {
        _sqsClient.Dispose();
        await _valkeyConnection.DisposeAsync();
    }
}