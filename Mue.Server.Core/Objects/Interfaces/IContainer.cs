using System.Collections.Generic;
using System.Threading.Tasks;
using Mue.Server.Core.Models;

namespace Mue.Server.Core.Objects
{
    public interface IContainer
    {
        Task<IEnumerable<ObjectId>> GetContents(GameObjectType? type = null);
        Task<ObjectId> Find(string term, GameObjectType? type = null);
        Task<ObjectId> FindIn(string term, GameObjectType? type = null);
    }
}