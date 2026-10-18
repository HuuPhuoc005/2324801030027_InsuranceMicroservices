namespace PricingService;

public partial class Program
{
    public static Task Main(string[] args) => CreateWebHostBuilder(args).Build().RunAsync();

    // Preserve the host entry point used by the lecturer's Alba integration fixture.
    public static IHostBuilder CreateWebHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(web => web.UseStartup<Startup>());
}
