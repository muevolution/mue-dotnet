using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mue.Server.Core;
using Mue.Server.Tools;

static IHostBuilder CreateHostBuilder(ToolCliOptions opts) =>
    Host.CreateDefaultBuilder()
        .ConfigureHostConfiguration(opts.AddCliOptions)
        .ConfigureAppConfiguration(builder =>
        {
            builder.AddJsonFile("appsettings.json", optional: true);
            builder.AddJsonFile("appsettings.Development.json", optional: true);
            builder.AddEnvironmentVariables();
        })
        .ConfigureMueStartup()
        .ConfigureServices(services =>
        {
            services.AddHostedService<ToolHost>();
        });

await Parser.Default.ParseArguments<ToolCliOptions>(args)
    .WithParsedAsync(async o =>
    {
        var host = CreateHostBuilder(o);
        await host.RunConsoleAsync();
    });
