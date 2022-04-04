using Mue.Scripting;

namespace Mue.Server.Core.Scripting.Implementation;

public class MueScriptTest
{
    public MueScriptTest(IWorld world, MueEngineExecutor executor)
    {
    }

    [MueExposedScriptMethod]
    public int HandlesArguments(string message)
    {
        return message.Length;
    }

    [MueExposedScriptMethod]
    public void ReturnsVoid()
    {
        // nop
    }

    [MueExposedScriptMethod]
    public uint ReturnsNumber()
    {
        return 4; // randomly selected
    }

    [MueExposedScriptMethod]
    public string ReturnsString()
    {
        return "asdf";
    }

    [MueExposedScriptMethod]
    public List<string> ReturnsStringList()
    {
        return new List<string> { "a", "b", "c" };
    }

    [MueExposedScriptMethod]
    public Task ReturnsTask()
    {
        return Task.CompletedTask;
    }

    [MueExposedScriptMethod]
    public Task<string[]> ReturnsTaskStringArray()
    {
        return Task.FromResult(new[] { "a", "b", "c" });
    }

    [MueExposedScriptMethod]
    public Task ReturnsWayTooLate()
    {
        return Task.Delay(5000);
    }

    [MueExposedScriptMethod]
    public dynamic DynamicDictionaryLocksSuccessfully()
    {
        dynamic output = new DynamicDictionary();
        output.SampleValue = "a";
        ((DynamicDictionary)output).Lock();
        return output;
    }
}
