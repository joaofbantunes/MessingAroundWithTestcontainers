using Amazon.SQS;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests;

public class WebApplicationFactoryBuilder
{
    private readonly Dictionary<string, string?> _configuration = new();
    private readonly List<Action<IServiceCollection>> _serviceConfigurations = new();

    public static WebApplicationFactoryBuilder Create() => new();

    public WebApplicationFactoryBuilder WithAwsSqs(string serviceUrl)
    {
        _serviceConfigurations.Add(services =>
        {
            services.RemoveAll<IAmazonSQS>();
            services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(new AmazonSQSConfig
            {
                ServiceURL = serviceUrl
            }));
        });
        return this;
    }

    public WebApplicationFactoryBuilder WithConfiguration(string key, string? value)
    {
        _configuration[key] = value;
        return this;
    }

    public WebApplicationFactory<Program> Build()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // this code runs as the host is built, so it's too late to add the configuration
                // that is used before the host is built (i.e. while registering services in DI)
                // more info: https://github.com/dotnet/aspnetcore/issues/37680

                builder.ConfigureAppConfiguration((ctx, config) => config.AddInMemoryCollection(_configuration));

                builder.ConfigureServices(services =>
                {
                    foreach (var serviceConfiguration in _serviceConfigurations)
                    {
                        serviceConfiguration(services);
                    }
                });
            });
    }
}