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
    public class TopicPublisherServiceTests
    {
        private string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        private string topicName = "topic.1";
        private string subscriptionName = "topicpublisher-subscription";

        [Fact]
        public async Task Topic_PublishMessage()
        {
            var host = HostBuilder();
            // Clear any previous test messages
            TopicPublisherService.PublishedMessages.Clear();
         
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var service = host.Services.GetRequiredService<TopicPublisherService>();
            await service.ExecuteAsync(cts.Token);
            var messages = TopicPublisherService.PublishedMessages;

            try
            {
                string testMessage = $"test-message-published";
                
                await using var client = new ServiceBusClient(connectionString);
                ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName, new ServiceBusReceiverOptions
                {
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

                var responses = await receiver.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(5));

                // Verify the message was processed and modified
                Assert.Equal($"{testMessage}", responses[0].Body.ToString());
            }
            finally
            {
                cts.Dispose();
                TopicPublisherService.PublishedMessages.Clear();
            }
        }

        private IHost HostBuilder()
        {
            return new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(sp => new TopicPublisherService(
                        connectionString,
                        topicName,
                        subscriptionName,
                        sp.GetRequiredService<ILogger<TopicPublisherService>>()
                    ));
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddDebug();
                })
                .Build();
        }
    }
}
