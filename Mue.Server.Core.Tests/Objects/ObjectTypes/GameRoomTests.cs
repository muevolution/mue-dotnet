using System;
using System.Threading.Tasks;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Tests;
using Xunit;

public class GameRoomTests
{
    private SystemMock _sys;

    public GameRoomTests()
    {
        _sys = new SystemMock();
    }

    [Fact]
    public async Task CreateRoom()
    {
        var creatorId = new ObjectId("p:root");
        var parentId = new ObjectId("r:root");

        var actual = await GameRoom.Create(_sys.World.Object, "RoomCreate", creatorId, parentId);

        Assert.Equal("RoomCreate", actual.Name);
        Assert.Equal(creatorId, actual.Meta.Creator);
    }

    [Fact]
    public async Task ImitateRoom()
    {
        var actual = await GameRoom.Imitate(_sys.World.Object, new ObjectId("r:test"));

        Assert.NotNull(actual.Id);
        Assert.Equal(GameObjectType.Room, actual.ObjectType);
        Assert.Equal("r:test", actual.Id.Id);
    }

    // Find

    [Fact(Skip = "TODO")]
    public void Find() { }
}