using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Tests;
using Mue.Server.Core.Utils;
using Xunit;

public class GameObjectTests
{
    private SystemMock _sys;

    private readonly static ObjectId DefaultMockId = new ObjectId("i:test");
    private readonly static ObjectId DefaultRoom = new ObjectId("r:root");

    private readonly static ObjectMetadata DefaultMetadata = new ObjectMetadata
    {
        Name = "Test name",
        Creator = new ObjectId("p:root"),
        Location = DefaultRoom,
        Parent = DefaultRoom,
    };

    private GameObject CreateMock(ObjectMetadata meta = null, ObjectId id = null)
    {
        if (meta == null)
        {
            meta = DefaultMetadata;
        }
        if (id == null)
        {
            id = DefaultMockId;
        }

        return new GameObjectMock(_sys.World.Object, id == ObjectId.Empty ? GameObjectType.Item : id.ObjectType, meta, id);
    }

    public GameObjectTests()
    {
        _sys = new SystemMock();
    }

    // MetaGeneric

    [Fact]
    public void MetaGenericGetter()
    {
        var mock = CreateMock();

        var actual = mock.MetaBasic;
        Assert.Equal(DefaultMetadata, actual);
    }

    // ObjectType

    [Fact]
    public void ObjectTypeGetter()
    {
        var mock = CreateMock();

        var actual = mock.ObjectType;
        Assert.Equal(GameObjectType.Item, actual);
    }

    // IsPendingAdd

    [Fact]
    public void IsPendingAddWhenIdSet()
    {
        var mock = CreateMock(null);

        var actual = mock.IsPendingAdd;
        Assert.False(actual);
    }

    [Fact]
    public void IsPendingAddWhenIdUnset()
    {
        var objId = ObjectId.Empty;
        var mock = CreateMock(null, objId);

        var actual = mock.IsPendingAdd;
        Assert.True(actual);
    }

    [Fact]
    public void IsPendingAddWhenIdNull()
    {
        var mock = CreateMock(null, ObjectId.Empty);

        var actual = mock.IsPendingAdd;
        Assert.True(actual);
    }

    // SetInitialId

    [Fact]
    public void SetInitialIdFresh()
    {
        var mock = CreateMock(null, ObjectId.Empty);

        mock.SetInitialId("asdf");
        Assert.Equal(new ObjectId("i:asdf"), mock.Id);
    }

    [Fact]
    public void SetInitialIdAlreadySet()
    {
        var mock = CreateMock();

        Action actual = () => mock.SetInitialId("asdf");
        Assert.Throws<Exception>(actual);
    }

    [Fact]
    public void SetInitialIdEmpty()
    {
        var mock = CreateMock();

        Action actual = () => mock.SetInitialId(String.Empty);
        Assert.Throws<Exception>(actual);
    }

    // Name

    [Fact]
    public void NameGetter()
    {
        var mock = CreateMock(null, ObjectId.Empty);

        var actual = mock.Name;
        Assert.Equal("Test name", actual);
    }

    // Parent

    [Fact]
    public void ParentGetter()
    {
        var mock = CreateMock(null, ObjectId.Empty);

        var actual = mock.Parent;
        Assert.Equal(new ObjectId("r:root"), actual);
    }

    // IsParentRoot

    [Fact]
    public void IsParentRootIfMatches()
    {
        var meta = DefaultMetadata with { Parent = new ObjectId("i:test") };
        var mock = CreateMock(meta);

        var actual = mock.IsParentRoot;
        Assert.True(actual);
    }

    [Fact]
    public void IsParentRootIfNoMatch()
    {
        var mock = CreateMock();

        var actual = mock.IsParentRoot;
        Assert.False(actual);
    }

    // Location

    [Fact]
    public void LocationGetter()
    {
        var mock = CreateMock(null, ObjectId.Empty);

        var actual = mock.Location;
        Assert.Equal(new ObjectId("r:root"), actual);
    }

    // IsLocationRoot

    [Fact]
    public void IsLocationRootIfMatches()
    {
        var meta = DefaultMetadata with { Location = new ObjectId("i:test") };
        var mock = CreateMock(meta);

        var actual = mock.IsLocationRoot;
        Assert.True(actual);
    }

    [Fact]
    public void IsLocationRootIfNoMatch()
    {
        var mock = CreateMock();

        var actual = mock.IsLocationRoot;
        Assert.False(actual);
    }

    // GetProp

    [Fact]
    public async Task GetProp()
    {
        var expected = new PropValue("aaa");
        _sys.StorageManager.Setup(s => s.GetProp(DefaultMockId, "testpath")).ReturnsAsync(expected);

        var mock = CreateMock();

        var actual = await mock.GetProp("testpath");
        Assert.Equal(expected, actual);
    }

    // GetProps

    [Fact]
    public async Task GetProps()
    {
        var expected = new Dictionary<string, PropValue> {
            {"propname", new PropValue("aaa")},
        };

        _sys.StorageManager.Setup(s => s.GetProps(DefaultMockId)).ReturnsAsync(expected);

        var mock = CreateMock();

        var actual = await mock.GetProps();
        Assert.Equal(expected, actual);
    }

    // SetProp

    [Fact]
    public async Task SetProp()
    {
        var expected = new PropValue("aaa");
        _sys.StorageManager.Setup(s => s.SetProp(DefaultMockId, "testpath", expected)).ReturnsAsync(true);

        var mock = CreateMock();

        var actual = await mock.SetProp("testpath", expected);
        Assert.True(actual);
    }

    // SetProps

    [Fact]
    public async Task SetProps()
    {
        var expected = new Dictionary<string, PropValue> {
            {"propname", new PropValue("aaa")},
        };

        _sys.StorageManager.Setup(s => s.SetProps(DefaultMockId, expected)).ReturnsAsync(true);

        var mock = CreateMock();

        var actual = await mock.SetProps(expected);
        Assert.True(actual);
    }

    // MatchName

    [Fact]
    public void MatchNameExact()
    {
        var mock = CreateMock();

        var actual = mock.MatchName("TeST naMe ");
        Assert.True(actual);
    }

    [Theory]
    [InlineData("another name")]
    [InlineData(null)]
    public void MatchNameUnmatched(string name)
    {
        var mock = CreateMock();

        var actual = mock.MatchName(name);
        Assert.False(actual);
    }

    // Rename

    [Fact]
    public async Task Rename()
    {
        var objId = new ObjectId("p:test");
        var mock = CreateMock(id: objId);

        _sys.StorageManager.Setup(s => s.GetMeta<ObjectMetadata>(objId)).ReturnsAsync(mock.Meta);
        _sys.StorageManager.Setup(s => s.UpdatePlayerNameIndex(objId, "Test name", "Newname")).ReturnsAsync(true);

        var actual = await mock.Rename("Newname");
        Assert.True(actual);

        var newMeta = mock.Meta with { Name = "Newname" };
        _sys.StorageManager.Verify(v => v.UpdateMeta(objId, newMeta));
        _sys.StorageManager.Verify(v => v.UpdatePlayerNameIndex(objId, "Test name", "Newname"));

        _sys.World.Verify(v => v.FireObjectEvent<IObjectUpdateResult>(mock.Id, "rename", new RenameResult("Test name", "Newname"), false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RenameFailsOnBlank(string name)
    {
        var mock = CreateMock();

        var actual = await mock.Rename(name);
        Assert.False(actual);

        _sys.StorageManager.VerifyNoOtherCalls();
    }

    // Reparent
    // Move

    // MoveFinish

    [Fact]
    public void MoveFinish()
    {
        var newLocationId = new ObjectId("r:new");
        var oldLocationId = new ObjectId("r:old");

        var mock = CreateMock();

        var expectedMoveResult = new MoveResult(oldLocationId, newLocationId);
        var expectedObjectUpdate = new ObjectUpdate(mock.Id, "move", expectedMoveResult);

        var actual = mock.MoveFinish(newLocationId, oldLocationId);
        Assert.Equal(expectedMoveResult, actual);

        _sys.World.Verify(v => v.FireObjectEvent<IObjectUpdateResult>(mock.Id, "move", new MoveResult(oldLocationId, newLocationId), false));
    }

    // Destroy

    [Fact]
    public async Task Destroy()
    {
        var mock = CreateMock();

        var actual = await mock.Destroy();
        Assert.True(actual);

        _sys.StorageManager.Verify(v => v.DestroyObject(mock));
        _sys.ObjectCache.Verify(v => v.OnDestroy(mock));
    }

    // Invalidate

    [Fact]
    public async Task Invalidate()
    {
        var mock = CreateMock();

        var expectedMeta = new ObjectMetadata
        {
            Name = "Some replacement meta",
            Location = new ObjectId("r:newtest"),
        };

        _sys.StorageManager.Setup(s => s.GetMeta<ObjectMetadata>(mock.Id)).ReturnsAsync(expectedMeta);

        var actual = await mock.Invalidate();
        Assert.True(actual);
        Assert.Equal(expectedMeta, mock.Meta);
    }

    [Fact]
    public async Task InvalidateAfterDeletion()
    {
        var mock = CreateMock();

        Func<Task<bool>> actual = () => mock.Invalidate();
        await Assert.ThrowsAsync<GameObjectIdDoesNotExistException>(actual);
    }
}