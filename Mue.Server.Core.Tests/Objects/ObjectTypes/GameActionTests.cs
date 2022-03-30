using System;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Tests;
using Mue.Server.Core.Utils;
using Xunit;

public class GameActionTests
{
    private SystemMock _sys;

    public GameActionTests()
    {
        _sys = new SystemMock();
    }

    [Fact]
    public async Task CreateAction()
    {
        var creatorId = new ObjectId("p:root");

        var actual = await GameAction.Create(_sys.World.Object, "ActionCreate", creatorId);

        Assert.Equal("ActionCreate", actual.Name);
        Assert.Equal(creatorId, actual.Meta.Creator);
    }

    [Fact]
    public async Task CreateActionBannedNames()
    {
        var creatorId = new ObjectId("p:root");

        Func<Task<GameAction>> actual = () => GameAction.Create(_sys.World.Object, "$test", creatorId);
        await Assert.ThrowsAsync<IllegalObjectNameException>(actual);
    }

    [Fact]
    public async Task ImitateAction()
    {
        var actual = await GameAction.Imitate(_sys.World.Object, new ObjectId("a:test"));

        Assert.NotNull(actual.Id);
        Assert.Equal(GameObjectType.Action, actual.ObjectType);
        Assert.Equal("a:test", actual.Id.Id);
    }

    // SetTarget

    [Fact]
    public async Task SetTarget()
    {
        var objId = new ObjectId("a:test");
        var newTarget = new ObjectId("r:newtarget");
        var action = GameObjectMocker.CreateRealAction(_sys.World.Object, objId, "Test");

        var actual = await action.SetTarget(newTarget);

        Assert.True(actual);
        Assert.Equal(newTarget, action.Target);
        _sys.StorageManager.Verify(v => v.UpdateMeta<ActionMetadata>(objId, It.Is<ActionMetadata>(v => v.Target == newTarget)));
    }

    [Fact]
    public async Task SetTargetThrowsWithInvalidType()
    {
        var objId = new ObjectId("a:test");
        var newTarget = new ObjectId("p:newtarget");
        var action = GameObjectMocker.CreateRealAction(_sys.World.Object, objId, "Test");

        Func<Task<bool>> actual = () => action.SetTarget(newTarget);
        await Assert.ThrowsAsync<InvalidGameObjectTargetException>(actual);
    }

    // MatchCommand

    [Fact]
    public void MatchCommandMatchesFullCommand()
    {
        var objId = new ObjectId("a:test");
        var action = GameObjectMocker.CreateRealAction(_sys.World.Object, objId, "TestSingle");

        var actual = action.MatchCommand("testsingle");
        Assert.True(actual);
    }

    [Theory]
    [InlineData("testmulti")]
    [InlineData("double")]
    [InlineData("triple")]
    public void MatchCommandMatchesPartialCommand(string matchCommand)
    {
        var objId = new ObjectId("a:test");
        var action = GameObjectMocker.CreateRealAction(_sys.World.Object, objId, "TestMulti;Double;Triple");

        var actual = action.MatchCommand(matchCommand);
        Assert.True(actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void MatchCommandNoMatcExpected(string matchCommand)
    {
        var objId = new ObjectId("a:test");
        var action = GameObjectMocker.CreateRealAction(_sys.World.Object, objId, "TestSingle");

        var actual = action.MatchCommand(matchCommand);
        Assert.False(actual);
    }

    [Fact]
    public void MatchCommandNoMatchWrongFullCommand()
    {
        var objId = new ObjectId("a:test");
        var action = GameObjectMocker.CreateRealAction(_sys.World.Object, objId, "TestSingle");

        var actual = action.MatchCommand("invalid");
        Assert.False(actual);
    }

    [Fact]
    public void MatchCommandNoMatchWrongPartialCommand()
    {
        var objId = new ObjectId("a:test");
        var action = GameObjectMocker.CreateRealAction(_sys.World.Object, objId, "TestMulti;Double;Triple");

        var actual = action.MatchCommand("invalid");
        Assert.False(actual);
    }
}