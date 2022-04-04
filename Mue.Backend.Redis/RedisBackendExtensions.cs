using Microsoft.Extensions.DependencyInjection;
using Mue.Backend.PubSub;
using Mue.Backend.Storage;

namespace Mue.Backend;

public static class RedisBackendExtensions
{
    public static IServiceCollection AddRedisBackendServices(this IServiceCollection services, Func<IServiceProvider, string> connectionStringBuilder)
    {
        services.AddSingleton<RedisBackend>((s) => new RedisBackend(connectionStringBuilder(s)));
        services.AddSingleton<IBackendStorage, RedisStorage>();
        services.AddSingleton<IBackendPubSub, RedisPubSub>();

        return services;
    }
}
