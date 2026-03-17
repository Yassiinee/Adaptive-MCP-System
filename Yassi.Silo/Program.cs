using Orleans.Configuration;

internal class Program
{
    private static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo
            // In-process localhost clustering for dev — swap to Redis/Azure for prod
            .UseLocalhostClustering()
            .AddMemoryGrainStorage("yassiStore")
            .Configure<ClusterOptions>(opts =>
            {
                opts.ClusterId = "yassi-dev";
                opts.ServiceId = "YassiService";
            });
    })
    .Build();

        await host.RunAsync();
    }
}