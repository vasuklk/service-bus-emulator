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
            "admin-subscription",
            sp.GetRequiredService<ILogger<TopicListenerService>>()
        ));
        services.AddHostedService(sp => sp.GetRequiredService<TopicListenerService>());
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

await host.RunAsync();
