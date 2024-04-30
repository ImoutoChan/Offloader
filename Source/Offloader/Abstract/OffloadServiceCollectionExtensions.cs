using Microsoft.Extensions.DependencyInjection;

namespace Offloader;

public static class OffloadServiceCollectionExtensions
{
    public static IServiceCollection AddOffload<T>(
        this IServiceCollection services, 
        Action<OffloadOptions<T>> configure)
    {
        services.Configure(configure);
        
        services.AddSingleton<Offloader<T>>();
        services.AddTransient<IOffloader<T>>(x => x.GetRequiredService<Offloader<T>>());
        services.AddTransient<IOffloadReader<T>>(x => x.GetRequiredService<Offloader<T>>());
        services.AddHostedService<OffloadHostedService<T>>();

        return services;
    }

    public static IServiceCollection AddOffload<TItem, TItemProcessorService>(
        this IServiceCollection services, 
        Action<OffloadOptions<TItem>>? configure = null) 
        where TItemProcessorService : class, IOffloadItemProcessor<TItem>
    {
        if (configure != null)
            services.Configure(configure);

        services.AddTransient<IOffloadItemProcessor<TItem>, TItemProcessorService>();
        services.Configure<OffloadOptions<TItem>>(x => x.UseItemProcessor<TItemProcessorService>());

        services.AddSingleton<Offloader<TItem>>();
        services.AddTransient<IOffloader<TItem>>(x => x.GetRequiredService<Offloader<TItem>>());
        services.AddTransient<IOffloadReader<TItem>>(x => x.GetRequiredService<Offloader<TItem>>());
        services.AddHostedService<OffloadHostedService<TItem>>();

        return services;
    }
}
