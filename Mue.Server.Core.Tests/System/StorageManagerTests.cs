using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;
using Mue.Server.Core.Tests;
using Mue.Server.Core.Utils;
using Xunit;

public class StorageManagerTests
{
    private SystemMock _sys = new SystemMock();

    // AddObject

    [Fact]
    public async Task AddObjectWithSetId()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);
        obj.SetupGet(s => s.Name).Returns("Test room");
        obj.SetupMeta(new ObjectMetadata
        {
            Name = "Test room",
            Creator = new ObjectId("p:creator"),
            Parent = new ObjectId("r:parent"),
        });

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.AddObject(obj.Object);
        Assert.True(actual);

        _sys.BackendStorage.Storage.Verify(v => v.SetContains("i:r:all", "r:test"));
        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:r:test:meta", "name", "Test room"));
        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:r:test:meta", "creator", "p:creator"));
        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:r:test:meta", "parent", "r:parent"));
        _sys.BackendStorage.Transact.Verify(v => v.SetAdd("i:r:all", "r:test"));
    }

    [Fact]
    public async Task AddObjectWithUnsetId()
    {
        var objId = ObjectId.Empty;
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);
        obj.SetupGet(s => s.Name).Returns("Test room");
        obj.SetupMeta(new ObjectMetadata
        {
            Name = "Test room",
            Creator = new ObjectId("p:creator"),
            Parent = new ObjectId("r:parent"),
        });
        obj.SetupGet(s => s.IsPendingAdd).Returns(true);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.AddObject(obj.Object);
        Assert.True(actual);

        obj.VerifyGet(v => v.IsPendingAdd);
        obj.Verify(v => v.SetInitialId(It.IsAny<string>()));
    }

    [Fact]
    public async Task AddObjectAlreadyExists()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);

        _sys.BackendStorage.Storage.Setup(s => s.SetContains("i:r:all", objId.Id)).ReturnsAsync(true);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        Func<Task<bool>> actual = () => manager.AddObject(obj.Object);
        await Assert.ThrowsAsync<GameObjectIdExistsException>(actual);
    }

    [Fact]
    public async Task AddObjectUnsetName()
    {
        var objId = ObjectId.Empty;
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);
        obj.SetupGet(s => s.Name).Returns("");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        Func<Task<bool>> actual = () => manager.AddObject(obj.Object);
        await Assert.ThrowsAsync<InvalidGameObjectNameException>(actual);
    }

    [Fact]
    public async Task AddObjectExistingPlayerName()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateMock<GamePlayer>(objId);
        obj.SetupGet(s => s.Name).Returns("Playername");

        _sys.BackendStorage.Storage.Setup(s => s.HashGetField("i:p:names", "playername")).ReturnsAsync("p:exists");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        Func<Task<bool>> actual = () => manager.AddObject(obj.Object);
        await Assert.ThrowsAsync<PlayerNameAlreadyExistsException>(actual);
    }

    // DestroyObject

    [Fact]
    public async Task DestroyObjectStandard()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);
        obj.SetupGet(s => s.Location).Returns(new ObjectId("r:loc"));

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.DestroyObject(obj.Object);
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.KeyDelete("s:r:test:props"));
        _sys.BackendStorage.Transact.Verify(v => v.KeyDelete("s:r:test:contents"));
        _sys.BackendStorage.Transact.Verify(v => v.KeyDelete("s:r:test:meta"));
        _sys.BackendStorage.Transact.Verify(v => v.SetRemove("i:r:all", objId.Id));
        _sys.BackendStorage.Transact.Verify(v => v.SetRemove("s:r:loc:contents", "r:test"));
    }

    [Fact]
    public async Task DestroyObjectPlayer()
    {
        var objId = new ObjectId("p:test");
        var obj = GameObjectMocker.CreateMock<GamePlayer>(objId);
        obj.SetupGet(s => s.Name).Returns("Playername");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.DestroyObject(obj.Object);
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.HashDeleteField("i:p:names", "playername"));
    }

    [Fact]
    public async Task DestroyObjectScript()
    {
        var objId = new ObjectId("s:test");
        var obj = GameObjectMocker.CreateMock<GameScript>(objId);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.DestroyObject(obj.Object);
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.KeyDelete("s:s:test:script"));
    }

    // DoesObjectExist

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DoesObjectExist(bool serverResponse)
    {
        var objId = new ObjectId("r:test");

        _sys.BackendStorage.Storage.Setup(s => s.SetContains("i:r:all", objId.Id)).ReturnsAsync(serverResponse);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.DoesObjectExist(objId);
        Assert.Equal(serverResponse, actual);
    }

    // GetAllPlayers

    [Fact]
    public async Task GetAllPlayers()
    {
        _sys.BackendStorage.Storage.Setup(s => s.HashGetAll("i:p:names")).ReturnsAsync(new Dictionary<string, string> {
            {"p:test1", "Test 1"},
            {"p:test2", "Test 2"},
        });

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetAllPlayers();
        Assert.Equal(2, actual.Count);
        Assert.Contains(new KeyValuePair<string, string>("p:test1", "Test 1"), actual);
        Assert.Contains(new KeyValuePair<string, string>("p:test2", "Test 2"), actual);
    }

    // FindPlayerByName

    [Fact]
    public async Task FindPlayerByNameExists()
    {
        _sys.BackendStorage.Storage.Setup(s => s.HashGetField("i:p:names", "playername")).ReturnsAsync("p:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.FindPlayerByName("Playername");
        Assert.Equal("p:test", actual);

        _sys.BackendStorage.Storage.Verify(v => v.HashGetField("i:p:names", "playername"));
    }

    [Fact]
    public async Task FindPlayerByNameNotExist()
    {
        _sys.BackendStorage.Storage.Setup(s => s.HashGetField("i:p:names", It.IsAny<string>())).ReturnsAsync((string)null);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.FindPlayerByName("Playername");
        Assert.Null(actual);

        _sys.BackendStorage.Storage.Verify(v => v.HashGetField("i:p:names", "playername"));
    }

    // UpdatePlayerNameIndex

    [Fact]
    public async Task UpdatePlayerNameIndex()
    {
        var objId = new ObjectId("p:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.UpdatePlayerNameIndex(objId, "OldPlayerName", "NewPlayerName");
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.HashDeleteField("i:p:names", "oldplayername"));
        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("i:p:names", "newplayername", "p:test"));
    }

    // GetProp

    [Fact]
    public async Task GetPropExists()
    {
        var objId = new ObjectId("p:test");
        var expected = new PropValue(3);

        _sys.BackendStorage.Storage.Setup(s => s.HashGetField("s:p:test:props", "proppath")).ReturnsAsync("3");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetProp(objId, "proppath");
        Assert.Equal(expected, actual);
        Assert.Equal(PropValueType.Number, actual.ValueType);
        Assert.Equal(3, actual.NumberValue);
    }

    [Fact]
    public async Task GetPropNotExists()
    {
        var objId = new ObjectId("p:test");

        _sys.BackendStorage.Storage.Setup(s => s.HashGetField("s:p:test:props", "proppath")).ReturnsAsync((string)null);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetProp(objId, "proppath");
        Assert.True(actual.IsNull);
    }

    // GetProps

    [Fact]
    public async Task GetProps()
    {
        var objId = new ObjectId("p:test");

        _sys.BackendStorage.Storage.Setup(s => s.HashGetAll("s:p:test:props")).ReturnsAsync(new Dictionary<string, string> {
            {"prop1", "\"valueA\""},
            {"prop2", "123"},
        });

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetProps(objId);
        Assert.Collection(actual, new Action<KeyValuePair<string, PropValue>>[] {
           p => { Assert.Equal("prop1", p.Key); Assert.Equal(new PropValue("valueA"), p.Value); },
           p => { Assert.Equal("prop2", p.Key); Assert.Equal(new PropValue(123), p.Value); },
        });
    }

    // SetProp

    [Fact]
    public async Task SetProp()
    {
        var objId = new ObjectId("p:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.SetProp(objId, "proppath", new PropValue("newvalue"));
        Assert.True(actual);

        _sys.BackendStorage.Storage.Verify(v => v.HashSetField("s:p:test:props", "proppath", "\"newvalue\""));
    }

    [Fact]
    public async Task SetPropUnset()
    {
        var objId = new ObjectId("p:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.SetProp(objId, "proppath", new PropValue());
        Assert.True(actual);

        _sys.BackendStorage.Storage.Verify(v => v.HashDeleteField("s:p:test:props", "proppath"));
    }

    // SetProps

    [Fact]
    public async Task SetProps()
    {
        var objId = new ObjectId("p:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.SetProps(objId, new Dictionary<string, PropValue> {
            {"prop1", new PropValue("valueA")},
            {"prop2", new PropValue(123)},
        });
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.KeyDelete("s:p:test:props"));
        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:p:test:props", "prop1", "\"valueA\""));
        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:p:test:props", "prop2", "123"));
    }

    // GetContents

    [Fact]
    public async Task GetContents()
    {
        var objId = new ObjectId("r:test");

        _sys.BackendStorage.Storage.Setup(s => s.SetMembers("s:r:test:contents")).ReturnsAsync(new List<string> {
            "i:test1",
            "i:test2",
            "a:test3"
        });

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetContents(objId);
        Assert.Equal(3, actual.Count());
        Assert.Contains(actual, p => p == new ObjectId("i:test1"));
        Assert.Contains(actual, p => p == new ObjectId("i:test2"));
        Assert.Contains(actual, p => p == new ObjectId("a:test3"));
    }

    [Fact]
    public async Task GetContentsOfType()
    {
        var objId = new ObjectId("r:test");

        _sys.BackendStorage.Storage.Setup(s => s.SetMembers("s:r:test:contents")).ReturnsAsync(new List<string> {
            "i:test1",
            "i:test2",
            "a:test3"
        });

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetContents(objId, GameObjectType.Item);
        Assert.Equal(2, actual.Count());
        Assert.Contains(actual, p => p == new ObjectId("i:test1"));
        Assert.Contains(actual, p => p == new ObjectId("i:test2"));
    }

    // ReparentObject

    [Fact]
    public async Task ReparentObject()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.ReparentObject(obj.Object.Id, new ObjectId("r:newparent"), new ObjectId("r:oldparent"));
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:r:test:meta", "parent", "r:newparent"));
    }

    // MoveObject

    [Fact]
    public async Task MoveObject()
    {
        var objId = new ObjectId("r:test");
        var obj = GameObjectMocker.CreateMock<GameRoom>(objId);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.MoveObject(obj.Object.Id, new ObjectId("r:newloc"), new ObjectId("r:oldloc"));
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:r:test:meta", "location", "r:newloc"));
        _sys.BackendStorage.Transact.Verify(v => v.SetRemove("s:r:oldloc:contents", "r:test"));
        _sys.BackendStorage.Transact.Verify(v => v.SetAdd("s:r:newloc:contents", "r:test"));
    }

    // MoveObjects

    [Fact]
    public async Task MoveObjects()
    {
        var objId1 = new ObjectId("r:test1");
        var obj1 = GameObjectMocker.CreateMock<GameRoom>(objId1);
        var objId2 = new ObjectId("r:test2");
        var obj2 = GameObjectMocker.CreateMock<GameRoom>(objId2);

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.MoveObjects(new[] { obj1.Object.Id, obj2.Object.Id }, new ObjectId("r:newloc"), new ObjectId("r:oldloc"));
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:r:test1:meta", "location", "r:newloc"));
        _sys.BackendStorage.Transact.Verify(v => v.SetRemove("s:r:oldloc:contents", "r:test1"));
        _sys.BackendStorage.Transact.Verify(v => v.SetAdd("s:r:newloc:contents", "r:test1"));
        _sys.BackendStorage.Transact.Verify(v => v.HashSetField("s:r:test2:meta", "location", "r:newloc"));
        _sys.BackendStorage.Transact.Verify(v => v.SetRemove("s:r:oldloc:contents", "r:test2"));
        _sys.BackendStorage.Transact.Verify(v => v.SetAdd("s:r:newloc:contents", "r:test2"));
    }

    // GetMeta

    [Fact]
    public async Task GetMetaTyped()
    {
        var objId = new ObjectId("p:test");

        _sys.BackendStorage.Storage.Setup(s => s.HashGetAll("s:p:test:meta")).ReturnsAsync(new Dictionary<string, string> {
            {"name", "Player name"},
            {"password_hash", "passwordHash1"},
        });
        var expected = new PlayerMetadata
        {
            Name = "Player name",
            PasswordHash = "passwordHash1",
        };

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetMeta<PlayerMetadata>(objId);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetMetaStandard()
    {
        var objId = new ObjectId("r:test");

        _sys.BackendStorage.Storage.Setup(s => s.HashGetAll("s:r:test:meta")).ReturnsAsync(new Dictionary<string, string> {
            {"name", "Room name"},
        });
        var expected = new ObjectMetadata
        {
            Name = "Room name",
        };

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetMeta(objId);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetMetaKey()
    {
        var objId = new ObjectId("r:test");

        _sys.BackendStorage.Storage.Setup(s => s.HashGetField("s:r:test:meta", "name")).ReturnsAsync("Room name");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.GetMeta(objId, "name");
        Assert.Equal("Room name", actual);
    }

    // UpdateMeta

    [Fact]
    public async Task UpdateMetaObject()
    {
        var objId = new ObjectId("r:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        var actual = await manager.UpdateMeta(objId, new ObjectMetadata
        {
            Name = "Sample name",
        });
        Assert.True(actual);

        _sys.BackendStorage.Transact.Verify(s => s.KeyDelete("s:r:test:meta"));
        _sys.BackendStorage.Transact.Verify(s => s.HashSetField("s:r:test:meta", "name", "Sample name"));
    }

    [Fact]
    public async Task UpdateMetaKey()
    {
        var objId = new ObjectId("r:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        _sys.BackendStorage.Storage.Setup(s => s.HashSetField("s:r:test:meta", "name", "Sample name")).ReturnsAsync(true);

        var actual = await manager.UpdateMeta(objId, "name", "Sample name");
        Assert.True(actual);
    }

    // GetRootValue

    [Fact]
    public async Task GetRootValue()
    {
        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        _sys.BackendStorage.Storage.Setup(s => s.HashGetField("i:root", "root_room")).ReturnsAsync("r:superroot");

        var actual = await manager.GetRootValue(RootField.RootRoom);
        Assert.Equal("r:superroot", actual);
    }

    // SetRootValue

    [Fact]
    public async Task SetRootValue()
    {
        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        _sys.BackendStorage.Storage.Setup(s => s.HashSetField("i:root", "root_room", "r:superroot")).ReturnsAsync(true);

        var actual = await manager.SetRootValue(RootField.RootRoom, "r:superroot");
        Assert.True(actual);
    }

    // GetScriptCode

    [Fact]
    public async Task GetScriptCode()
    {
        var objId = new ObjectId("s:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        _sys.BackendStorage.Storage.Setup(s => s.KeyGet("s:s:test:script")).ReturnsAsync("sample");

        var actual = await manager.GetScriptCode(objId);
        Assert.Equal("sample", actual);
    }

    // SetScriptCode

    [Fact]
    public async Task SetScriptCode()
    {
        var objId = new ObjectId("s:test");

        var manager = new StorageManager(_sys.BackendStorage.Storage.Object);

        _sys.BackendStorage.Storage.Setup(s => s.KeySet("s:s:test:script", "sample")).ReturnsAsync(true);

        var actual = await manager.SetScriptCode(objId, "sample");
        Assert.True(actual);
    }
}
