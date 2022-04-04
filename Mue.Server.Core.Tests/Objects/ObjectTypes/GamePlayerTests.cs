public class GamePlayerTests
{
    private SystemMock _sys;

    public GamePlayerTests()
    {
        _sys = new SystemMock();
    }

    [Fact]
    public async Task CreatePlayer()
    {
        var creatorId = new ObjectId("p:root");
        var parentId = new ObjectId("r:root");

        var actual = await GamePlayer.Create(_sys.World.Object, "PlayerCreate", "pw", creatorId, parentId);

        Assert.Equal("PlayerCreate", actual.Name);
        Assert.Equal(creatorId, actual.Meta.Creator);
        Assert.NotNull(actual.Meta.PasswordHash);
        Assert.NotEqual("pw", actual.Meta.PasswordHash);
    }

    [Fact]
    public async Task ImitatePlayer()
    {
        var actual = await GamePlayer.Imitate(_sys.World.Object, new ObjectId("p:test"));

        Assert.NotNull(actual.Id);
        Assert.Equal(GameObjectType.Player, actual.ObjectType);
        Assert.Equal("p:test", actual.Id.Id);
    }

    // Move

    [Fact(Skip = "TODO")]
    public void Move() { }

    // Find

    [Fact(Skip = "TODO")]
    public void Find() { }

    // ResolveTarget

    [Fact(Skip = "TODO")]
    public void ResolveTarget() { }

    // SendMessage

    [Fact(Skip = "TODO")]
    public void SendMessage() { }

    // Quit

    [Fact(Skip = "TODO")]
    public void Quit() { }

    // CheckPassword

    [Fact(Skip = "TODO")]
    public void CheckPassword() { }

}
