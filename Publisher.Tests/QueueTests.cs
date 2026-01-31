using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Xunit;

namespace Publisher.Tests
{
    public class QueueTests
    {
        private string GetConnectionString() =>
            Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTIONSTRING")
            ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        [Fact]
        public async Task SendAndReceiveMessage_ToQueue2()
        {
            string connectionString = GetConnectionString();
            string queueName = "queue.2";
            string body = $"test-message-{Guid.NewGuid()}";

            await using var client = new ServiceBusClient(connectionString);
            ServiceBusSender sender = client.CreateSender(queueName);
            var messageId = Guid.NewGuid().ToString();
            var message = new ServiceBusMessage(body) { MessageId = messageId };
            await sender.SendMessageAsync(message);

            ServiceBusReceiver receiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

            ServiceBusReceivedMessage received = null;
            var timeout = TimeSpan.FromSeconds(10);
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var r = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1));
                if (r == null) continue;
                if (r.MessageId == messageId)
                {
                    received = r;
                    await receiver.CompleteMessageAsync(r);
                    break;
                }
                else
                {
                    // Not our message; abandon so others can process
                    await receiver.AbandonMessageAsync(r);
                }
            }

            Assert.NotNull(received);
            Assert.Equal("body", received.Body.ToString());
        }
    }
}
