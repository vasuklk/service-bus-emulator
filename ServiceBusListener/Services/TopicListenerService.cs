using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceBusListener.Services
{
    public class TopicListenerService : BackgroundService
    {
        private readonly string _connectionString;
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly ILogger<TopicListenerService> _logger;
        private ServiceBusClient? _client;
        private ServiceBusProcessor? _processor;

        public TopicListenerService(string connectionString, string topicName, string subscriptionName, ILogger<TopicListenerService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _subscriptionName = subscriptionName ?? throw new ArgumentNullException(nameof(subscriptionName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TopicListenerService starting...");

            try
            {
                _client = new ServiceBusClient(_connectionString);
                _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());

                _processor.ProcessMessageAsync += MessageHandler;
                _processor.ProcessErrorAsync += ErrorHandler;

                await _processor.StartProcessingAsync(stoppingToken);
                _logger.LogInformation("TopicListenerService started successfully");

                // Keep the service running until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TopicListenerService cancellation requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TopicListenerService failed: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                string body = args.Message.Body.ToString();
                _logger.LogInformation("Received message: {Body}", body);

                // Modify the message content
                string modifiedBody = body + " [PROCESSED]";
                _logger.LogInformation("Modified message: {ModifiedBody}", modifiedBody);

                // Store the modified content for test verification
                ProcessedMessages.Add(modifiedBody);

                // Complete the message
                await args.CompleteMessageAsync(args.Message);
                _logger.LogInformation("Message processed and completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", ex.Message);
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Error in topic listener: {ErrorSource}", args.ErrorSource);
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TopicListenerService stopping...");

            if (_processor != null)
            {
                await _processor.StopProcessingAsync(cancellationToken);
                await _processor.DisposeAsync();
            }

            if (_client != null)
            {
                await _client.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }

        public static List<string> ProcessedMessages { get; } = new();
    }
}
