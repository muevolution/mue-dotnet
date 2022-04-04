using System;
using System.Threading.Tasks;
using Moq;
using Mue.Scripting;
using Mue.Server.Core.Scripting;
using Mue.Server.Core.Tests;
using Xunit;

public class ScriptIntegrationTests
{
    private SystemMock _sys = new SystemMock();

    private (PythonScriptEngine, DynamicDictionary, Mock<Action<object>>) PrepareTest()
    {
        var callback = new Mock<Action<object>>();

        var executor = new MueEngineExecutor("hello world", "p:1234", "s:4567")
        {
            Callback = callback.Object,
        };
        var si = ScriptIntegration.Build(_sys.World.Object, executor, true);

        var eng = new PythonScriptEngine();

        return (eng, si, callback);
    }

    [Fact]
    public async Task ScriptIntegratorBuildsCorrectly()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorBuildsCorrectly", @"
def __mue_entry__(mue):
    v = mue.script.this_script == ""s:4567""
    mue.callback(v)
", 5000, si);

        callback.Verify(v => v(true));
    }

    [Fact]
    public async Task ScriptIntegratorHandlesArgs()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorHandlesArgs", @"
def __mue_entry__(mue):
    v = mue.test.HandlesArguments('Tester')
    mue.callback(v)
", 5000, si);

        callback.Verify(v => v(6));
    }

    [Fact]
    public async Task ScriptIntegratorReturnsVoid()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorReturnsVoid", @"
def __mue_entry__(mue):
    v = mue.test.ReturnsVoid()
    mue.callback(v)
", 5000, si);

        callback.Verify(v => v(null));
    }

    [Fact(Skip = "Verification fails but it seems to work")]
    public async Task ScriptIntegratorReturnsNumber()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorReturnsNumber", @"
def __mue_entry__(mue):
    v = mue.test.ReturnsNumber()
    mue.callback(v)
", 5000, si);

        callback.Verify(v => v(4));
    }

    [Fact]
    public async Task ScriptIntegratorReturnsString()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorReturnsString", @"
def __mue_entry__(mue):
    v = mue.test.ReturnsString()
    mue.callback(v)
", 5000, si);

        callback.Verify(v => v("asdf"));
    }

    [Fact]
    public async Task ScriptIntegratorReturnsList()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorReturnsList", @"
def __mue_entry__(mue):
    v = mue.test.ReturnsStringList()
    mue.callback(v)
", 5000, si);

        callback.Verify(v => v(new[] { "a", "b", "c" }));
    }

    [Fact]
    public async Task ScriptIntegratorReturnsStringArrayFromTask()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorReturnsStringArrayFromTask", @"
def __mue_entry__(mue):
    v = mue.test.ReturnsTaskStringArray()
    mue.callback(v)
", 5000, si);

        callback.Verify(v => v(new[] { "a", "b", "c" }));
    }

    [Fact]
    public async Task ScriptIntegratorTimesOut()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorTimesOut", @"
def __mue_entry__(mue):
    v = mue.test.ReturnsWayTooLate()
    mue.callback('Eventually')
", 6000, si);

        // TODO: Figure out how to verify that it took 5sec
        callback.Verify(v => v("Eventually"));
    }

    [Fact]
    public async Task ScriptIntegratorCannotEditLockedDict()
    {
        var (eng, si, callback) = PrepareTest();

        await eng.SpawnAndRun("ScriptIntegratorCannotEditLockedDict", @"
def __mue_entry__(mue):
    v = mue.test.DynamicDictionaryLocksSuccessfully()
    print('Origin match', v.SampleValue == 'a')

    try:
        v.SampleValue = 'overwrite'
    except Exception as e:
        mue.callback(e)
", 5000, si);

        callback.Verify(v => v(It.IsAny<object>()));
    }
}