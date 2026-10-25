using EasyNetQ;
namespace ChatService.Messaging.RabbitMq;
public static class RabbitInstaller
{
    public static IServiceCollection AddRabbitListeners(this IServiceCollection services, RabbitMqOptions options)
    {
        options ??= new RabbitMqOptions();
        services.AddSingleton(options);
        if (!options.Enabled) return services;
        services.AddSingleton<IBus>(_ => RabbitHutch.CreateBus(
            $"host={options.Host};port={options.Port};username={options.Username};password={options.Password}"));
        services.AddSingleton<RabbitEventListener>();
        return services;
    }
}
public static class RabbitListenersInstaller
{
    public static void UseRabbitListeners(this IApplicationBuilder app, List<Type> eventTypes)
    {
        if (app.ApplicationServices.GetRequiredService<RabbitMqOptions>().Enabled)
            app.ApplicationServices.GetRequiredService<RabbitEventListener>().ListenTo(eventTypes);
    }
}
