using System;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mue.Server.Core;

namespace Mue.Server.Tools
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<ToolCliOptions>(args)
                .WithParsedAsync<ToolCliOptions>(async o =>
                {
                    var host = CreateHostBuilder(o);
                    await host.RunConsoleAsync();
                });
        }

        private static IHostBuilder CreateHostBuilder(ToolCliOptions opts) =>
            Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(opts.AddCliOptions)
                .ConfigureAppConfiguration(builder =>
                {
                    // TODO: Figure out how to load the standard config file for the server
                    builder.AddJsonFile("appsettings.json");
                    builder.AddJsonFile("appsettings.Local.json", optional: true);
                })
                .ConfigureMueStartup()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<ToolHost>();
                });
    }
}
