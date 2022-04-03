using System;
using System.Threading.Tasks;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Tests;
using Xunit;

public class ObjectIdPropListHelperTests
{
    private SystemMock _sys;

    public ObjectIdPropListHelperTests()
    {
        _sys = new SystemMock();
    }

    [Fact]
    public async Task AddNew()
    {
        var target = new ObjectId("p:0");
        var testVal = new ObjectId("r:1");
        var storedVal = new PropValue(new[] { new FlatPropValue(testVal) });

        var helper = new ObjectIdPropListHelper(_sys.World.Object, target, "testkey");
        await helper.Add(testVal);

        _sys.StorageManager.Verify(v => v.GetProp(target, "testkey"), Times.Once);
        _sys.StorageManager.Verify(v => v.SetProp(target, "testkey", storedVal), Times.Once);
    }

    [Fact]
    public async Task AddExisting()
    {
        var target = new ObjectId("p:0");
        var testVal = new ObjectId("r:1");
        var storedVal = new PropValue(new[] { new FlatPropValue(testVal) });

        _sys.StorageManager.Setup(s => s.GetProp(target, "testkey")).ReturnsAsync(storedVal);

        var helper = new ObjectIdPropListHelper(_sys.World.Object, target, "testkey");
        await helper.Add(testVal);

        _sys.StorageManager.Verify(v => v.GetProp(target, "testkey"), Times.Once);
        _sys.StorageManager.Verify(v => v.SetProp(target, "testkey", It.IsAny<PropValue>()), Times.Never);
    }

    [Fact]
    public async Task RemoveExisting()
    {
        var target = new ObjectId("p:0");
        var testVal = new ObjectId("r:1");
        var storedVal = new PropValue(new[] { new FlatPropValue(testVal) });

        _sys.StorageManager.Setup(s => s.GetProp(target, "testkey")).ReturnsAsync(storedVal);

        var helper = new ObjectIdPropListHelper(_sys.World.Object, target, "testkey");
        await helper.Remove(testVal);

        _sys.StorageManager.Verify(v => v.GetProp(target, "testkey"), Times.Once);
        _sys.StorageManager.Verify(v => v.SetProp(target, "testkey", PropValue.EmptyList), Times.Once);
    }

    [Fact]
    public async Task RemoveNonExisting()
    {
        var target = new ObjectId("p:0");
        var testVal = new ObjectId("r:1");

        _sys.StorageManager.Setup(s => s.GetProp(target, "testkey"));

        var helper = new ObjectIdPropListHelper(_sys.World.Object, target, "testkey");
        await helper.Remove(testVal);

        _sys.StorageManager.Verify(v => v.GetProp(target, "testkey"), Times.Once);
        _sys.StorageManager.Verify(v => v.SetProp(target, "testkey", new PropValue()), Times.Never);
    }

    [Fact]
    public async Task Contains()
    {
        var target = new ObjectId("p:0");
        var testVal = new ObjectId("r:1");
        var storedVal = new PropValue(new[] { new FlatPropValue(testVal) });

        _sys.StorageManager.Setup(s => s.GetProp(target, "testkey")).ReturnsAsync(storedVal);

        var helper = new ObjectIdPropListHelper(_sys.World.Object, target, "testkey");
        var actual1 = await helper.Contains(testVal);
        Assert.True(actual1);

        var actual2 = await helper.Contains(new ObjectId("s:000"));
        Assert.False(actual2);

        _sys.StorageManager.Verify(v => v.GetProp(target, "testkey"), Times.Exactly(2));
    }

    [Fact]
    public async Task All()
    {
        var target = new ObjectId("p:0");
        var testVal = new ObjectId("r:1");
        var storedVal = new PropValue(new[] { new FlatPropValue(testVal) });

        _sys.StorageManager.Setup(s => s.GetProp(target, "testkey")).ReturnsAsync(storedVal);

        var helper = new ObjectIdPropListHelper(_sys.World.Object, target, "testkey");
        var actual = await helper.All();
        Assert.Equal(new[] { testVal }, actual);

        _sys.StorageManager.Verify(v => v.GetProp(target, "testkey"), Times.Once);
    }
}