using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using StackExchange.Redis;
using static Api.SampleKeyGenerator;

namespace Api;

public class SqsBackgroundService(
    IAmazonSQS sqsClient,
    IConfiguration configuration,
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<SqsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = configuration.GetValue<string>("SqsQueueName")!;
        var receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = queueName,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 30
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiveMessageResponse = await sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

            foreach (var message in receiveMessageResponse.Messages)
            {
                logger.LogInformation("Message received: {Message}", message.Body);
                await connectionMultiplexer.GetDatabase().StringSetAsync(
                    new RedisKey(GenerateKey(message.Body)),
                    new RedisValue(message.Body)
                );
            }

            if (receiveMessageResponse.Messages.Count > 0)
            {
                await sqsClient.DeleteMessageBatchAsync(new DeleteMessageBatchRequest
                {
                    QueueUrl = queueName,
                    Entries = receiveMessageResponse.Messages.Select(m => new DeleteMessageBatchRequestEntry
                    {
                        Id = m.MessageId,
                        ReceiptHandle = m.ReceiptHandle
                    }).ToList()
                }, stoppingToken);
            }
        }
    }
}