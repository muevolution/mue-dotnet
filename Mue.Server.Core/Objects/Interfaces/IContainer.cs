namespace Mue.Server.Core.Objects;

public interface IContainer
{
    Task<IEnumerable<ObjectId>> GetContents(GameObjectType? type = null);
    Task<ObjectId?> FindIn(string term, GameObjectType? type = null);
    Task<ObjectId?> FindActionIn(string term, bool searchItems = false);
}
