using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceBusListener.Services;
using Xunit;

namespace Publisher.Tests
{
    public class TopicListenerTests
    {
        private string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        private string topicName = "topic.1";
        private string subscriptionName = "topiclistener-subscription";

        [Fact]
        public async Task TopicListener_ReceivesAndProcessesMessage()
        {
            string testMessage = $"test-message-{Guid.NewGuid()}";

            await using var client = new ServiceBusClient(connectionString);

            // Create topic and subscription if they don't exist
            var sender = client.CreateSender(topicName);

            // Clear any previous test messages
            TopicListenerService.ProcessedMessages.Clear();

            // Start the listener service
            var host = HostBuilder();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var hostTask = host.StartAsync(cts.Token);

            try
            {
                // Wait for service to start
                await Task.Delay(2000);

                // Send a test message to the topic
                var message = new ServiceBusMessage(testMessage)
                {
                    MessageId = Guid.NewGuid().ToString()
                };
                message.ApplicationProperties["MessageType"] = "TopicListener";
                
                await sender.SendMessageAsync(message);

                // Wait for the message to be processed
                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                while (DateTime.UtcNow < deadline)
                {
                    if (TopicListenerService.ProcessedMessages.Contains($"{testMessage} [PROCESSED]"))
                    {
                        break;
                    }
                    await Task.Delay(500);
                }
                // Verify the message was processed and modified
                Assert.Contains($"{testMessage} [PROCESSED]", TopicListenerService.ProcessedMessages);
            }
            finally
            {
                // Stop the service
                await sender.CloseAsync();
                await client.DisposeAsync();

                cts.Cancel();
                TopicListenerService.ProcessedMessages.Clear();
            }
        }

        private IHost HostBuilder()
        {
            return new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(sp => new TopicListenerService(
                        connectionString,
                        topicName,
                        subscriptionName,
                        sp.GetRequiredService<ILogger<TopicListenerService>>()
                    ));
                    services.AddHostedService(sp => sp.GetRequiredService<TopicListenerService>());
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddDebug();
                })
                .Build();
        }
    }
}
