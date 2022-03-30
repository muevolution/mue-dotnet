using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Tests;
using Xunit;

public class ContainerTests
{
    private SystemMock _sys;

    public ContainerTests()
    {
        _sys = new SystemMock();
    }

    // GetContents

    [Fact]
    public async Task GetContents()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateRealRoom(_sys.World.Object, objId, "Test room");

        var expected = new[] { new ObjectId("p:asdf"), new ObjectId("i:wsad") };
        _sys.StorageManager.Setup(s => s.GetContents(objId, GameObjectType.Room)).ReturnsAsync(expected);

        var actual = await obj.GetContents(GameObjectType.Room);
        Assert.Equal(expected, actual);
    }

    // Find [just calls FindIn, overrides need to be tested]

    // FindIn

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, "", null)]
    [InlineData(null, "NoWork", null)]
    [InlineData("p:test", "sampleplayer", null)]
    [InlineData("p:test", "sampleplayer", GameObjectType.Player)]
    [InlineData(null, "sampleplayer", GameObjectType.Item)]
    [InlineData("i:inner", "child item", null)]
    [InlineData(null, "child item", GameObjectType.Player)]
    [InlineData("i:inner", "child item", GameObjectType.Item)]
    public async Task FindIn(string expectedId, string term, GameObjectType? type = null)
    {
        var testPlayerId = new ObjectId("p:test");
        var testPlayer = GameObjectMocker.CreateRealPlayer(_sys.World.Object, testPlayerId, "SamplePlayer");

        var testItemId = new ObjectId("i:test");
        var testItem = GameObjectMocker.CreateRealItem(_sys.World.Object, testItemId, "TestItem");

        var testChildItemId = new ObjectId("i:inner");
        var testChildItem = GameObjectMocker.CreateRealItem(_sys.World.Object, testChildItemId, "Child item", location: testItemId);

        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateRealRoom(_sys.World.Object, objId, "Test room");

        var contents = new[] { testPlayerId, testItemId };
        _sys.StorageManager.Setup(s => s.GetContents(objId, null)).ReturnsAsync(contents);
        _sys.StorageManager.Setup(s => s.GetContents(objId, GameObjectType.Player)).ReturnsAsync(new[] { testPlayerId });
        _sys.StorageManager.Setup(s => s.GetContents(objId, GameObjectType.Item)).ReturnsAsync(new[] { testItemId });

        _sys.StorageManager.Setup(s => s.GetContents(testItemId, null)).ReturnsAsync(new[] { testChildItemId });
        _sys.StorageManager.Setup(s => s.GetContents(testItemId, GameObjectType.Item)).ReturnsAsync(new[] { testChildItemId });

        _sys.World.Setup(s => s.GetObjectsById(contents)).ReturnsAsync(new IGameObject[] { testPlayer, testItem });
        _sys.World.Setup(s => s.GetObjectsById(new[] { testPlayerId })).ReturnsAsync(new IGameObject[] { testPlayer });
        _sys.World.Setup(s => s.GetObjectsById(new[] { testItemId })).ReturnsAsync(new IGameObject[] { testItem });
        _sys.World.Setup(s => s.GetObjectsById(new[] { testChildItemId })).ReturnsAsync(new IGameObject[] { testChildItem });

        var actual = await obj.FindIn(term, type);
        var expected = expectedId != null ? new ObjectId(expectedId) : null;
        Assert.Equal(expected, actual);
    }

    // Destroy

    [Fact]
    public async Task DestroySpillsContents()
    {
        var locationId = new ObjectId("r:parent");
        var testPlayerId = new ObjectId("p:test");
        var testPlayer = GameObjectMocker.CreateMock<GamePlayer>(testPlayerId);
        var testItemId = new ObjectId("i:test");
        var testItem = GameObjectMocker.CreateMock<GameItem>(testItemId);

        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateRealRoom(_sys.World.Object, objId, "Test room", location: locationId);

        var contents = new List<ObjectId> { testPlayerId, testItemId };
        _sys.StorageManager.Setup(s => s.GetContents(objId, null)).ReturnsAsync(contents);

        _sys.StorageManager.Setup(s => s.MoveObjects(contents, locationId, objId)).ReturnsAsync(true);
        _sys.World.Setup(s => s.GetObjectById(testPlayerId, null)).ReturnsAsync(testPlayer.Object);
        _sys.World.Setup(s => s.GetObjectById(testItemId, null)).ReturnsAsync(testItem.Object);

        var actual = await obj.Destroy();
        Assert.True(actual);

        testPlayer.Verify(v => v.MoveFinish(locationId, objId));
        testItem.Verify(v => v.MoveFinish(locationId, objId));
    }
}