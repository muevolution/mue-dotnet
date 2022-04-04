namespace Mue.Server.Core.Objects;

public class GameItem : Container
{
    public static Task<GameItem> Create(IWorld world, string name, ObjectId creator, ObjectId? location = null)
    {
        if (name.StartsWith("$"))
        {
            throw new IllegalObjectNameException(name, GameObjectType.Action);
        }

        var p = new GameItem(world, new ObjectMetadata
        {
            Name = name,
            Creator = creator,
            Parent = creator,
            Location = location,
        });

        return world.ObjectCache.StandardCreate(p);
    }

    public static Task<GameItem> Imitate(IWorld world, ObjectId id)
    {
        return world.ObjectCache.StandardImitate<GameItem>(id, (meta) => Task.FromResult(new GameItem(world, meta, id)));
    }

    protected GameItem(IWorld world, ObjectMetadata meta, ObjectId? id = null) : base(world, GameObjectType.Item, meta, id) { }
}
