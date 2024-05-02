using Amazon.SQS;
using Api;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddAWSService<IAmazonSQS>()
    .AddHostedService<SqsBackgroundService>()
    .AddSingleton<IConnectionMultiplexer>(
        s => ConnectionMultiplexer.Connect(s.GetRequiredService<IConfiguration>().GetValue<string>("ValkeyEndpoint")!));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

// ReSharper disable once ClassNeverInstantiated.Global - needed for tests with WebApplicationFactory
public sealed partial class Program
{
}