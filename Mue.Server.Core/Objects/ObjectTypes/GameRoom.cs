namespace Mue.Server.Core.Objects;

public class GameRoom : Container
{
    public static Task<GameRoom> Create(IWorld world, string name, ObjectId creator, ObjectId parent, ObjectId? location = null)
    {
        var p = new GameRoom(world, new ObjectMetadata
        {
            Name = name,
            Creator = creator,
            Parent = parent,
            Location = location ?? parent ?? null,
        });

        return world.ObjectCache.StandardCreate(p);
    }

    public static Task<GameRoom> RootCreate(IWorld world, string name)
    {
        var player0 = new ObjectId("p:0");
        var room0 = new ObjectId("r:0");

        var p = new GameRoom(world, new ObjectMetadata
        {
            Name = name,
            Creator = player0,
            Parent = room0,
            Location = room0,
        }, room0);

        return world.ObjectCache.StandardCreate(p);
    }

    public static Task<GameRoom> Imitate(IWorld world, ObjectId id)
    {
        return world.ObjectCache.StandardImitate<GameRoom>(id, (meta) => Task.FromResult(new GameRoom(world, meta, id)));
    }

    protected GameRoom(IWorld world, ObjectMetadata meta, ObjectId? id = null) : base(world, GameObjectType.Room, meta, id) { }
}
