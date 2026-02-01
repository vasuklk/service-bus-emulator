using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceBusListener.Services;

string connectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTIONSTRING")
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton(sp => new TopicListenerService(
            connectionString,
            "topic.1",
            "topiclistener-subscription",
            sp.GetRequiredService<ILogger<TopicListenerService>>()
        ));
        services.AddHostedService(sp => sp.GetRequiredService<TopicListenerService>());
        services.AddSingleton(sp => new TopicPublisherService(
            connectionString,
            "topic.1",
            "topicpublisher-subscription",
            sp.GetRequiredService<ILogger<TopicPublisherService>>()
        ));
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// TopicPublisherService publisherService = new TopicPublisherService(
//     connectionString,
//     "topic.1",
//     "admin-subscription",
//     host.Services.GetRequiredService<ILogger<TopicPublisherService>>()
// );
// publisherService.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

await host.RunAsync();
