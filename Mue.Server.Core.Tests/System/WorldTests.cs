using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mue.Common.Models;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;
using Mue.Server.Core.Tests;
using Mue.Server.Core.Utils;
using Xunit;

public class WorldTests
{
    private SystemMock _sys;

    private async Task<World> CreateMock(bool skipInit = false)
    {
        var logger = new NullLogger<World>();
        var sysFac = new SystemFactory(_sys.ServiceProvider);
        var world = new World(logger, sysFac, _sys.BackendStorage.Storage.Object, _sys.BackendPubSub.Object);

        if (!skipInit)
        {
            await world.Init();
        }

        return world;
    }

    public WorldTests()
    {
        _sys = new SystemMock();
        
        TimeUtil.InTestMode = true; // Freeze clock
    }

    // Init

    [Fact]
    public async Task Init()
    {
        var world = await CreateMock(true);

        await world.Init();

        var publishedJson = $"{{\"instance_id\":\"{world.WorldInstanceId}\",\"event_time\":\"{TimeUtil.FrozenTimeString}\",\"event_name\":\"joined\"}}";
        _sys.BackendPubSub.Verify(v => v.Publish("c:isc", publishedJson));
        _sys.BackendPubSub.Verify(v => v.Subscribe("c:isc", It.IsAny<Action<string, string>>()));

        // TODO: Figure out how to check the _hasInit flag status
    }

    // Shutdown

    [Fact]
    public async Task Shutdown()
    {
        var world = await CreateMock();

        await world.Shutdown();

        // TODO: Figure out how to check the _hasShutdown flag status
    }

    // PublishMessage

    [Fact]
    public async Task PublishMessageUntargeted()
    {
        var world = await CreateMock();

        var actual = await world.PublishMessage("Sample message");
        Assert.True(actual);

        var expectedJson = @"{""message"":""Sample message""}";
        _sys.BackendPubSub.Verify(v => v.Publish("c:world", expectedJson));
    }

    [Fact]
    public async Task PublishMessageToRoom()
    {
        var objId = new ObjectId("r:test");
        var room = GameObjectMocker.CreateMock<GameRoom>(objId);

        var world = await CreateMock();

        var actual = await world.PublishMessage(new InteriorMessage
        {
            Message = "Sample message"
        }, room.Object);
        Assert.True(actual);

        var expectedJson = @"{""message"":""Sample message""}";
        _sys.BackendPubSub.Verify(v => v.Publish("c:r:test", expectedJson));
    }

    // PlayerCommand

    [Fact]
    public async Task PlayerCommand()
    {
        var world = await CreateMock();
        var player = GameObjectMocker.CreateRealPlayer(world, new ObjectId("p:test"), "Test player");

        var cmd = new CommandRequest
        {
            Command = "Hello world!",
        };

        _sys.CommandProcessor.Setup(s => s.ProcessCommand(player, cmd)).ReturnsAsync(true);

        var actual = await world.PlayerCommand(player, cmd);
        Assert.True(actual);
    }

    // GetPlayerByName

    [Fact]
    public async Task GetPlayerByName()
    {
        var objId = new ObjectId("p:test");

        var world = await CreateMock();
        var player = GameObjectMocker.CreateRealPlayer(world, objId, "Test player");

        _sys.StorageManager.Setup(s => s.FindPlayerByName("TestPlayer")).ReturnsAsync("p:test");
        _sys.ObjectCache.Setup(s => s.StandardImitate<GamePlayer, PlayerMetadata>(objId, It.IsAny<Func<PlayerMetadata, Task<GamePlayer>>>())).ReturnsAsync(player);

        var actual = await world.GetPlayerByName("TestPlayer");
        Assert.Equal(player, actual);
    }

    // GetRootPlayer

    [Fact]
    public async Task GetRootPlayer()
    {
        var objId = new ObjectId("p:root");

        var world = await CreateMock();
        var player = GameObjectMocker.CreateRealPlayer(world, objId, "Test player");

        _sys.StorageManager.Setup(s => s.GetRootValue(RootField.God)).ReturnsAsync("p:root");
        _sys.ObjectCache.Setup(s => s.StandardImitate<GamePlayer, PlayerMetadata>(objId, It.IsAny<Func<PlayerMetadata, Task<GamePlayer>>>())).ReturnsAsync(player);

        var actual = await world.GetRootPlayer();
        Assert.Equal(player, actual);
    }

    // GetRootRoom

    [Fact]
    public async Task GetRootRoom()
    {
        var objId = new ObjectId("r:root");

        var world = await CreateMock();
        var room = GameObjectMocker.CreateRealRoom(world, objId, "Root room");

        _sys.StorageManager.Setup(s => s.GetRootValue(RootField.RootRoom)).ReturnsAsync("r:root");
        _sys.ObjectCache.Setup(s => s.StandardImitate<GameRoom>(objId, It.IsAny<Func<ObjectMetadata, Task<GameRoom>>>())).ReturnsAsync(room);

        var actual = await world.GetRootRoom();
        Assert.Equal(room, actual);
    }

    // GetStartRoom

    [Fact]
    public async Task GetStartRoom()
    {
        var objId = new ObjectId("r:start");

        var world = await CreateMock();
        var room = GameObjectMocker.CreateRealRoom(world, objId, "Start room");

        _sys.StorageManager.Setup(s => s.GetRootValue(RootField.StartRoom)).ReturnsAsync("r:start");
        _sys.ObjectCache.Setup(s => s.StandardImitate<GameRoom>(objId, It.IsAny<Func<ObjectMetadata, Task<GameRoom>>>())).ReturnsAsync(room);

        var actual = await world.GetStartRoom();
        Assert.Equal(room, actual);
    }

    // GetObjectById

    [Fact]
    public async Task GetObjectById()
    {
        var objId = new ObjectId("r:test");

        var world = await CreateMock();
        var room = GameObjectMocker.CreateRealRoom(world, objId, "Test room");

        _sys.ObjectCache.Setup(s => s.StandardImitate<GameRoom>(objId, It.IsAny<Func<ObjectMetadata, Task<GameRoom>>>())).ReturnsAsync(room);

        var actual = await world.GetObjectById(objId);
        Assert.Equal(room, actual);
    }

    // GetObjectsById

    [Fact]
    public async Task GetObjectsById()
    {
        var objId1 = new ObjectId("r:test1");
        var objId2 = new ObjectId("i:test2");

        var world = await CreateMock();
        var room = GameObjectMocker.CreateRealRoom(world, objId1, "Test room");
        var item = GameObjectMocker.CreateRealItem(world, objId2, "Test item");
        var expected = new IGameObject[] { room, item };

        _sys.ObjectCache.Setup(s => s.StandardImitate<GameRoom>(objId1, It.IsAny<Func<ObjectMetadata, Task<GameRoom>>>())).ReturnsAsync(room);
        _sys.ObjectCache.Setup(s => s.StandardImitate<GameItem>(objId2, It.IsAny<Func<ObjectMetadata, Task<GameItem>>>())).ReturnsAsync(item);

        var actual = await world.GetObjectsById(new[] { objId1, objId2 });
        Assert.Equal(expected, actual);
    }

    // GetActiveServers

    [Fact]
    public async Task GetActiveServers()
    {
        var world = await CreateMock();

        _sys.BackendPubSub.Setup(s => s.GetSubscribeCount("c:isc")).ReturnsAsync((uint)2);

        var actual = await world.GetActiveServers();
        Assert.Equal((uint)2, actual);
    }

    // GetActiveRoomIds

    [Fact]
    public async Task GetActiveRoomIds()
    {
        var world = await CreateMock();

        _sys.BackendPubSub.Setup(s => s.GetTopicsWildcard("c:r:*")).ReturnsAsync(new[] { "c:r:test1", "c:r:test2" });
        var expected = new ObjectId[] { new ObjectId("r:test1"), new ObjectId("r:test2") };

        var actual = await world.GetActiveRoomIds();
        Assert.Equal(expected, actual);
    }

    // GetConnectedPlayerIds

    [Fact]
    public async Task GetConnectedPlayerIds()
    {
        var world = await CreateMock();

        _sys.BackendPubSub.Setup(s => s.GetTopicsWildcard("c:p:*")).ReturnsAsync(new[] { "c:p:test1", "c:p:test2" });
        var expected = new ObjectId[] { new ObjectId("p:test1"), new ObjectId("p:test2") };

        var actual = await world.GetConnectedPlayerIds();
        Assert.Equal(expected, actual);
    }

    // InvalidateScriptCache

    [Fact]
    public async Task InvalidateScriptCache()
    {
        var world = await CreateMock();

        await world.InvalidateScriptCache();

        _sys.ObjectCache.Verify(v => v.InvalidateAll<GameScript>());

        var expectedJson = $"{{\"instance_id\":\"{world.WorldInstanceId}\",\"event_time\":\"{TimeUtil.FrozenTimeString}\",\"event_name\":\"invalidate_script\"}}";
        _sys.BackendPubSub.Verify(v => v.Publish("c:isc", expectedJson));
    }

    // StateEnforce (test something else by proxy)

    [Fact]
    public async Task StateEnforcedOnPreInit()
    {
        var world = await CreateMock(true);

        Func<Task> actual = () => world.InvalidateScriptCache();

        await Assert.ThrowsAsync<WorldNotInitException>(actual);
    }

    [Fact]
    public async Task StateEnforcedOnPostShutdown()
    {
        var world = await CreateMock();
        await world.Shutdown();

        Func<Task> actual = () => world.InvalidateScriptCache();

        await Assert.ThrowsAsync<WorldShutdownError>(actual);
    }
}