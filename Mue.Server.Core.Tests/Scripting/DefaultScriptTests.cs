using Mue.Scripting;
using Mue.Server.Core.Scripting;

public class DefaultScriptTests
{
    private const string PlayerIdStr = "p:1234";
    private static readonly ObjectId PlayerId = new ObjectId(PlayerIdStr);
    private const string ScriptIdStr = "s:4567";

    private SystemMock _sys = new SystemMock();

    private (PythonScriptEngine, DynamicDictionary) PrepareTest(string commandString, string commandArgs = null)
    {
        var executor = new MueEngineExecutor(commandString, PlayerIdStr, ScriptIdStr)
        {
            CommandArgs = commandArgs,
        };
        var si = ScriptIntegration.Build(_sys.World.Object, executor, true);

        var eng = new PythonScriptEngine();

        return (eng, si);
    }

    private async Task RunScript(PythonScriptEngine engine, DynamicDictionary si, string scriptName)
    {
        var scriptContent = await File.ReadAllTextAsync("../../../../Mue.Server.Core/Scripting/Defaults/" + scriptName);
        await engine.SpawnAndRun(scriptName, scriptContent, 5000, si);
    }

    [Fact]
    public async Task SayWorks()
    {
        var locId = new ObjectId("r:asdf");
        var loc = GameObjectMocker.CreateMock<GameRoom>(locId);

        _sys.World.Setup(s => s.GetObjectById(locId, null)).ReturnsAsync(loc.Object);

        var player = GameObjectMocker.CreateMock<GamePlayer>(PlayerId);
        player.SetupGet(s => s.Name).Returns("Kauko");
        player.SetupGet(s => s.Location).Returns(locId);

        _sys.World.Setup(s => s.GetObjectById(PlayerId, null)).ReturnsAsync(player.Object);

        var (eng, si) = PrepareTest("say", "Hello");
        await RunScript(eng, si, "say.py");

        Func<InteriorMessage, bool> verifyInteriorMessage = (im) =>
        {
            Assert.Equal(String.Empty, im.Message);
            Assert.Null(im.Meta);
            Assert.Equal("Hello", im.ExtendedContent["message"]);
            Assert.Equal(2, im.ExtendedContent.Count);
            Assert.Contains(new KeyValuePair<string, string>("message", "Hello"), im.ExtendedContent);
            Assert.Contains(new KeyValuePair<string, string>("speaker", PlayerIdStr), im.ExtendedContent);
            Assert.Equal("You say, \"{{message}}\"", im.ExtendedFormat.Value.FirstPerson);
            Assert.Equal("{{to_name speaker}} says, \"{{message}}\"", im.ExtendedFormat.Value.ThirdPerson);
            Assert.Equal(PlayerIdStr, im.Source);
            Assert.Equal(ScriptIdStr, im.Script);

            return true;
        };

        _sys.World.Verify(v => v.PublishMessage(It.Is<InteriorMessage>(v => verifyInteriorMessage(v)), (IGameObject)loc.Object));
    }

    [Fact]
    public async Task WhoWorks()
    {
        var player = GameObjectMocker.CreateMock<GamePlayer>(PlayerId);
        player.SetupGet(s => s.Name).Returns("Kauko");

        _sys.World.Setup(s => s.GetObjectById(PlayerId, null)).ReturnsAsync(player.Object);
        _sys.World.Setup(s => s.GetConnectedPlayerIds()).ReturnsAsync(new[] { PlayerId });

        var (eng, si) = PrepareTest("who");
        await RunScript(eng, si, "who.py");

        Func<InteriorMessage, bool> verifyInteriorMessage = (im) =>
        {
            Assert.Equal("Connected players: Kauko", im.Message);
            Assert.Null(im.Meta);
            Assert.Null(im.ExtendedContent);
            Assert.False(im.ExtendedFormat.HasValue);
            Assert.Equal(PlayerIdStr, im.Source);
            Assert.Equal(ScriptIdStr, im.Script);

            return true;
        };

        _sys.World.Verify(v => v.PublishMessage(It.Is<InteriorMessage>(v => verifyInteriorMessage(v)), (IGameObject)player.Object));
    }
}
