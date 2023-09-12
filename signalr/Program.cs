using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var serviceManager = new ServiceManagerBuilder()
            .WithOptions(option =>
            {
                option.ConnectionString = Environment.GetEnvironmentVariable("AzureSignalRConnectionString");
            })
            .BuildServiceManager();
        services.AddSingleton(serviceManager);
    })
    .Build();

host.Run();
