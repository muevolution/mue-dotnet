using System;
using System.Threading.Tasks;
using Moq;
using Mue.Scripting;
using Xunit;

public class ScriptEngineTests
{
    private (dynamic, Mock<Action<object>>) CreateIntegrator()
    {
        var callback = new Mock<Action<object>>();
        dynamic si = new DynamicDictionary();
        si.Callback = callback.Object;
        si.LimitedImplementation = true;

        return (si, callback);
    }

    [Fact]
    public async Task ScriptEngineWorks()
    {
        var (si, callback) = CreateIntegrator();
        var eng = new PythonScriptEngine();

        await eng.SpawnAndRun("ScriptEngineWorks", @"
def __mue_entry__(mue):
    mue.callback('Data value from Python')
", 1000, si);

        callback.Verify(v => v("Data value from Python"));
    }

    [Fact]
    public async Task ScriptEngineCatchesTimeOverrun()
    {
        var (si, callback) = CreateIntegrator();
        var eng = new PythonScriptEngine();

        Func<Task> actual = () => eng.SpawnAndRun("ScriptEngineCatchesTimeOverrun", @"
from time import sleep

def __mue_entry__(mue):
    sleep(5)
    mue.callback(None)
", 1000, si);

        await Assert.ThrowsAsync<TaskCanceledException>(actual);
        callback.VerifyNoOtherCalls();
    }

    [Fact(Skip = "This needs to pass but doesn't right now")]
    public async Task ScriptEngineNoImportClr()
    {
        var (si, callback) = CreateIntegrator();
        var eng = new PythonScriptEngine();

        Func<Task> actual = () => eng.SpawnAndRun("ScriptEngineNoImportClr", @"
def __mue_entry__(mue):
    import clr
    mue.callback(None)
", 1000, si);

        await Assert.ThrowsAsync<IronPython.Runtime.Exceptions.ImportException>(actual);
        callback.VerifyNoOtherCalls();
    }

    [Fact(Skip = "This may not be important, need to verify")]
    public async Task ScriptEngineNoImportSys()
    {
        var (si, callback) = CreateIntegrator();
        var eng = new PythonScriptEngine();

        Func<Task> actual = () => eng.SpawnAndRun("ScriptEngineNoImportSys", @"
def __mue_entry__(mue):
    import sys
    mue.callback(None)
", 1000, si);

        await Assert.ThrowsAsync<IronPython.Runtime.Exceptions.ImportException>(actual);
        callback.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ScriptEngineThrowsUsefulScriptError()
    {
        var (si, callback) = CreateIntegrator();
        var eng = new PythonScriptEngine();

        Func<Task> actual = () => eng.SpawnAndRun("ScriptEngineThrowsUsefulScriptError", @"
def __mue_entry__(mue):
    s = []
    s.push(invalid)
    mue.callback(None)
", 1000, si);

        var ex = await Assert.ThrowsAsync<MueScriptException>(actual);
        Console.WriteLine(ex.ScriptStackTrace);
        Assert.Equal(new[] {
            "Traceback (most recent call last):",
            "  File \"ScriptEngineThrowsUsefulScriptError\", line 4, in __mue_entry__",
            "AttributeError: 'list' object has no attribute 'push'",
        }, ex.ScriptStackTrace);

        callback.VerifyNoOtherCalls();
    }
}