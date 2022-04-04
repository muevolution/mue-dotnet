public class ObjectIdTests
{
    [Fact]
    public void ConstructorEmpty()
    {
        var actual = ObjectId.Empty;

        Assert.False(actual.IsAssigned);
        Assert.Null(actual.Id);
        Assert.Null(actual.ShortId);
        Assert.Equal(GameObjectType.Invalid, actual.ObjectType);
    }

    [Fact]
    public void ConstructorFullId()
    {
        var actual = new ObjectId("r:asdf");

        Assert.True(actual.IsAssigned);
        Assert.Equal("r:asdf", actual.Id);
        Assert.Equal("asdf", actual.ShortId);
        Assert.Equal(GameObjectType.Room, actual.ObjectType);
    }

    [Fact]
    public void ConstructorFullIdWithGoodCheck()
    {
        var actual = new ObjectId("r:asdf", GameObjectType.Room);

        Assert.True(actual.IsAssigned);
        Assert.Equal("r:asdf", actual.Id);
        Assert.Equal("asdf", actual.ShortId);
        Assert.Equal(GameObjectType.Room, actual.ObjectType);
    }

    [Fact]
    public void ConstructorFullIdWithBadCheck()
    {
        Func<ObjectId> actual = () => new ObjectId("r:asdf", GameObjectType.Script);
        Assert.Throws<IllegalObjectIdConstructorException>(actual);
    }

    [Fact]
    public void ConstructorWithOnlyId()
    {
        var actual = new ObjectId("asdf");

        Assert.False(actual.IsAssigned);
        Assert.Null(actual.Id);
        Assert.Equal("asdf", actual.ShortId);
        Assert.Equal(GameObjectType.Invalid, actual.ObjectType);
    }

    [Fact]
    public void ConstructorWithShortIdAndChecktype()
    {
        var actual = new ObjectId("asdf", GameObjectType.Room);

        Assert.True(actual.IsAssigned);
        Assert.Equal("r:asdf", actual.Id);
        Assert.Equal("asdf", actual.ShortId);
        Assert.Equal(GameObjectType.Room, actual.ObjectType);
    }
}
