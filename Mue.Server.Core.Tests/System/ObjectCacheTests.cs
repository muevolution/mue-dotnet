using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;
using Mue.Server.Core.Tests;
using Mue.Server.Core.Utils;
using Xunit;

public class ObjectCacheTests
{
    private SystemMock _sys = new SystemMock();

    // GetObject

    [Fact]
    public void GetObject()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");
        var ingest = new Dictionary<ObjectId, IGameObject> {
            {objId, obj},
        };

        var cache = new ObjectCache(_sys.World.Object, ingest);
        var actual = cache.GetObject(objId);

        Assert.NotNull(actual);
        Assert.IsType<GamePlayer>(actual);
        Assert.Same(obj, actual);
    }

    [Fact]
    public void GetObjectThatDoesNotExist()
    {
        var objId = new ObjectId("p:notexist");
        var cache = new ObjectCache(_sys.World.Object);
        var actual = cache.GetObject(objId);

        Assert.Null(actual);
    }

    [Fact]
    public void GetObjectWithType()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");

        var ingest = new Dictionary<ObjectId, IGameObject> { { objId, obj } };

        var cache = new ObjectCache(_sys.World.Object, ingest);
        var actual = cache.GetObject<GamePlayer>(objId);

        Assert.NotNull(actual);
        Assert.Same(obj, actual);
    }

    [Fact]
    public void GetObjectWithTypeThatDoesNotExist()
    {
        var objId = new ObjectId("p:test");
        var cache = new ObjectCache(_sys.World.Object);
        var actual = cache.GetObject<GamePlayer>(objId);

        Assert.Null(actual);
    }

    [Fact]
    public void GetObjectWithTypeMismatch()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");
        var ingest = new Dictionary<ObjectId, IGameObject> {
            {objId, obj},
        };

        var cache = new ObjectCache(_sys.World.Object, ingest);
        var actual = cache.GetObject<GameRoom>(new ObjectId("p:test"));

        Assert.Null(actual);
    }

    // HasObjectId

    [Fact]
    public void HasObjectId()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");
        var ingest = new Dictionary<ObjectId, IGameObject> {
            {objId, obj},
        };

        var cache = new ObjectCache(_sys.World.Object, ingest);
        var actual = cache.HasObjectId(objId);

        Assert.True(actual);
    }

    [Fact]
    public void HasObjectIdButDoesnt()
    {
        var cache = new ObjectCache(_sys.World.Object);
        var actual = cache.HasObjectId(new ObjectId("p:nothere"));

        Assert.False(actual);
    }

    // StandardCreate

    [Fact]
    public async Task StandardCreateWorks()
    {
        // TODO: Figure out how to merge this with the next test
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateRealRoom(_sys.World.Object, objId, "Test");

        var ingest = new Dictionary<ObjectId, IGameObject>();
        var cache = new ObjectCache(_sys.World.Object, ingest);

        var actual = await cache.StandardCreate<GameRoom>(obj);

        Assert.Equal(obj, actual);
        _sys.StorageManager.Verify(s => s.AddObject(obj));
        Assert.Contains(obj, ingest.Values);
    }

    [Fact]
    public async Task StandardCreateFailsIfPendingAdd()
    {
        var objId = ObjectId.Empty;
        var obj = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");

        var cache = new ObjectCache(_sys.World.Object);

        Func<Task<GamePlayer>> actual = () => cache.StandardCreate(obj);
        await Assert.ThrowsAsync<GameObjectIdDoesNotExistException>(actual);
    }

    [Fact]
    public async Task StandardCreateFailsIfIdExists()
    {
        var objId = new ObjectId("p:test");
        var obj1 = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");
        var obj2 = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");

        var ingest = new Dictionary<ObjectId, IGameObject>() {
            {objId, obj1},
        };
        var cache = new ObjectCache(_sys.World.Object, ingest);

        Func<Task<GamePlayer>> actual = () => cache.StandardCreate(obj2);
        await Assert.ThrowsAsync<GameObjectIdExistsException>(actual);
    }

    // StandardImitate

    [Fact]
    public async Task StandardImitateReturnsCached()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateRealRoom(_sys.World.Object, objId, "Test");

        var ingest = new Dictionary<ObjectId, IGameObject>() {
            {objId, obj},
        };
        var cache = new ObjectCache(_sys.World.Object, ingest);

        var mockRoom = GameObjectMocker.CreateMock<GameRoom>(objId);
        Func<ObjectMetadata, Task<GameRoom>> mockBuilder = (ObjectMetadata meta) => Task.FromResult(mockRoom.Object as GameRoom);

        var actual = await cache.StandardImitate<GameRoom>(objId, mockBuilder);
        Assert.Equal(obj, actual);
    }

    [Fact]
    public async Task StandardImitateBuildsNew()
    {
        var objId = new ObjectId("r:test");

        var ingest = new Dictionary<ObjectId, IGameObject>();
        var cache = new ObjectCache(_sys.World.Object, ingest);

        var roomName = "Test Room";
        _sys.StorageManager.Setup(s => s.GetMeta<ObjectMetadata>(objId)).ReturnsAsync(new ObjectMetadata
        {
            Name = roomName
        });
        Func<ObjectMetadata, Task<GameRoom>> mockBuilder = (ObjectMetadata meta) => Task.FromResult(GameObjectMocker.CreateRealRoom(_sys.World.Object, objId, meta.Name));

        var actual = await cache.StandardImitate<GameRoom>(objId, mockBuilder);
        Assert.Equal(actual.Id, objId);
        Assert.Equal(actual.Name, roomName);

        Assert.Contains(actual, ingest.Values);
    }

    [Fact]
    public async Task StandardImitateFailsIfIdNotExist()
    {
        var objId = new ObjectId("r:test");
        var cache = new ObjectCache(_sys.World.Object);

        Func<ObjectMetadata, Task<GameRoom>> mockBuilder = (ObjectMetadata meta) => Task.FromResult(GameObjectMocker.CreateRealRoom(_sys.World.Object, objId, meta.Name));

        Func<Task<GameRoom>> actual = () => cache.StandardImitate<GameRoom>(objId, mockBuilder);
        await Assert.ThrowsAsync<GameObjectIdDoesNotExistException>(actual);
    }

    // Invalidate

    [Fact]
    public async Task Invalidate()
    {
        var objId = new ObjectId("p:test");
        var cache = new ObjectCache(_sys.World.Object);
        await cache.Invalidate(objId);

        _sys.World.Verify(s => s.SendObjectUpdate(objId, "invalidate"));
    }

    // InvalidateLocal

    [Fact]
    public async Task InvalidateLocal()
    {
        var objId = new ObjectId("r:test");

        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);
        obj.Setup(s => s.Invalidate()).ReturnsAsync(true);

        var ingest = new Dictionary<ObjectId, IGameObject>() {
            {objId, obj.Object},
        };
        var cache = new ObjectCache(_sys.World.Object, ingest);

        var actual = await cache.InvalidateLocal(objId);
        Assert.True(actual);
        obj.Verify(v => v.Invalidate());
    }

    [Fact]
    public async Task InvalidateLocalReturnsFalseIfIdNotExist()
    {
        var objId = new ObjectId("r:test");
        var cache = new ObjectCache(_sys.World.Object);

        var actual = await cache.InvalidateLocal(objId);
        Assert.False(actual);
    }

    // InvalidateAll

    [Fact]
    public async Task InvalidateAllByEnum()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);
        obj.Setup(s => s.Invalidate()).ReturnsAsync(true);

        var dummyObjId = new ObjectId("p:test");
        var dummyObj = GameObjectMocker.CreateMock<GamePlayer>(dummyObjId);

        var ingest = new Dictionary<ObjectId, IGameObject>() {
            {objId, obj.Object},
            {dummyObjId, dummyObj.Object},
        };
        var cache = new ObjectCache(_sys.World.Object, ingest);

        var actual = await cache.InvalidateAll(GameObjectType.Room);
        Assert.Collection(actual, item => Assert.Equal(GameObjectType.Room, item.Key.ObjectType));
        obj.Verify(v => v.Invalidate());
        dummyObj.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidateAllByType()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);
        obj.Setup(s => s.Invalidate()).ReturnsAsync(true);

        var dummyObjId = new ObjectId("p:test");
        var dummyObj = GameObjectMocker.CreateMock<GamePlayer>(dummyObjId);

        var ingest = new Dictionary<ObjectId, IGameObject>() {
            {objId, obj.Object},
            {dummyObjId, dummyObj.Object},
        };
        var cache = new ObjectCache(_sys.World.Object, ingest);

        var actual = await cache.InvalidateAll<GameRoom>();
        Assert.Collection(actual, item => Assert.Equal(GameObjectType.Room, item.Key.ObjectType));
        obj.Verify(v => v.Invalidate());
        dummyObj.VerifyNoOtherCalls();
    }

    // OnDestroy

    [Fact]
    public async Task OnDestroy()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateMock<GamePlayer>(objId);
        obj.SetupGet(s => s.IsDestroyed).Returns(true);

        var cache = new ObjectCache(_sys.World.Object);
        await cache.OnDestroy(obj.Object);

        _sys.World.Verify(v => v.SendObjectUpdate(objId, "destroy"));
    }

    [Fact]
    public async Task OnDestroyFailsIfCalledOutOfOrder()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateMock<GamePlayer>(objId);
        obj.SetupGet(s => s.IsDestroyed).Returns(false);

        var cache = new ObjectCache(_sys.World.Object);
        Func<Task> actual = () => cache.OnDestroy(obj.Object);

        await Assert.ThrowsAsync<Exception>(actual);
    }

    // PostNetworkDestroy

    [Fact]
    public void PostNetworkDestroy()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateRealPlayer(_sys.World.Object, objId, "Test");
        var ingest = new Dictionary<ObjectId, IGameObject> {
            {objId, obj},
        };

        var cache = new ObjectCache(_sys.World.Object, ingest);
        cache.PostNetworkDestroy(objId);

        Assert.False(ingest.ContainsKey(objId));
    }
}