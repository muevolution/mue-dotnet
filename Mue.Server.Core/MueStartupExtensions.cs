using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mue.Backend;
using Mue.Scripting;
using Mue.Server.Core.ClientServer;
using Mue.Server.Core.Scripting;
using Mue.Server.Core.System.CommandBuiltins;
using Mue.Server.Hosts;

namespace Mue.Server.Core;

public static class MueStartupExtensions
{
    public static IHostBuilder ConfigureMueStartup(this IHostBuilder builder)
    {
        // This could theoretically live anywhere that can host Mue.Server.Core, not just in Mue.Server, but it's here for now due to IHostedService and IConfiguration usage
        return builder.ConfigureServices((services) =>
        {
            services.ConfigureMueServices();
        });
    }

    public static IServiceCollection ConfigureMueServices(this IServiceCollection services)
    {
        services.AddRedisBackendServices((s) => s.GetRequiredService<IConfiguration>()["RedisConnectionString"]);
        services.AddMueCoreServices();
        services.AddHostedService<HostedWorld>();

        return services;
    }

    public static IServiceCollection AddMueCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IWorld, World>();
        services.AddSingleton<ICommandProcessor, CommandProcessor>();
        services.AddSingleton<IObjectCache, ObjectCache>();
        services.AddSingleton<IStorageManager, StorageManager>();
        services.AddTransient<ISystemFactory, SystemFactory>();
        services.AddSingleton<IScriptManager, ScriptManager>();
        services.AddSingleton<IWorldFormatter, Formatter>();
        services.AddSingleton<IBuiltinCommands, BuiltinCommands>();
        services.AddScoped<IScriptEngine, PythonScriptEngine>();
        services.AddScoped<IClientToServer, ServerConnector>();

        return services;
    }
}
