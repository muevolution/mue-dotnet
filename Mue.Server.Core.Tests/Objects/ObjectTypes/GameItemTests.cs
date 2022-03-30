using System;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Tests;
using Mue.Server.Core.Utils;
using Xunit;

public class GameItemTests
{
    private SystemMock _sys;

    public GameItemTests()
    {
        _sys = new SystemMock();
    }

    [Fact]
    public async Task CreateItem()
    {
        var creatorId = new ObjectId("p:root");

        var actual = await GameItem.Create(_sys.World.Object, "ItemCreate", creatorId);

        Assert.Equal("ItemCreate", actual.Name);
        Assert.Equal(creatorId, actual.Meta.Creator);
    }

    [Fact]
    public async Task ImitateItem()
    {
        var actual = await GameItem.Imitate(_sys.World.Object, new ObjectId("i:test"));

        Assert.NotNull(actual.Id);
        Assert.Equal(GameObjectType.Item, actual.ObjectType);
        Assert.Equal("i:test", actual.Id.Id);
    }
}