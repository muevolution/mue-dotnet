using System;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Tests;
using Mue.Server.Core.Utils;
using Xunit;

public class GameScriptTests
{
    private SystemMock _sys;

    public GameScriptTests()
    {
        _sys = new SystemMock();
    }

    [Fact]
    public async Task CreateScript()
    {
        var creatorId = new ObjectId("p:root");

        var actual = await GameScript.Create(_sys.World.Object, "ScriptCreate", creatorId);

        Assert.Equal("ScriptCreate", actual.Name);
        Assert.Equal(creatorId, actual.Meta.Creator);
    }

    [Fact]
    public async Task ImitateScript()
    {
        var actual = await GameScript.Imitate(_sys.World.Object, new ObjectId("s:test"));

        Assert.NotNull(actual.Id);
        Assert.Equal(GameObjectType.Script, actual.ObjectType);
        Assert.Equal("s:test", actual.Id.Id);
    }

    // Invalidate

    [Fact]
    public async Task Invalidate()
    {
        var objId = new ObjectId("s:test");
        var script = GameObjectMocker.CreateRealScript(_sys.World.Object, objId, "Test script");

        var expectedMeta = new ObjectMetadata
        {
            Name = "Some replacement meta",
            Location = new ObjectId("r:newtest"),
        };

        _sys.StorageManager.Setup(s => s.GetMeta<ObjectMetadata>(script.Id)).ReturnsAsync(expectedMeta);
        _sys.StorageManager.Setup(s => s.GetScriptCode(objId)).ReturnsAsync("fun world");

        var actual = await script.Invalidate();
        Assert.True(actual);
        Assert.Equal(expectedMeta, script.Meta);
        Assert.Equal("fun world", script.Code);
    }
}