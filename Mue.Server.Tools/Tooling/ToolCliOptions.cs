using CommandLine;
using Microsoft.Extensions.Configuration;

namespace Mue.Server.Tools;

public class ToolCliOptions
{
    public ToolCliOptions(string task)
    {
        this.Task = task;
    }

    [Option(Required = true, HelpText = "Task to run")]
    public string Task { get; set; }

    public void AddCliOptions(IConfigurationBuilder config)
    {
        var dict = new Dictionary<string, string>();

        dict.Add("ToolSettings:Task", this.Task);

        config.AddInMemoryCollection(dict);
    }
}
