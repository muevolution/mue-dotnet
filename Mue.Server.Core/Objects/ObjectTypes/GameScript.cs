using System;
using System.Threading.Tasks;
using Mue.Server.Core.Models;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Objects
{
    public class GameScript : GameObject
    {
        public static Task<GameScript> Create(IWorld world, string name, ObjectId creator, ObjectId? location = null)
        {
            var p = new GameScript(world, new ObjectMetadata
            {
                Name = name,
                Creator = creator,
                Parent = creator,
                Location = location ?? creator,
            });

            return world.ObjectCache.StandardCreate(p);
        }

        public static Task<GameScript> Imitate(IWorld world, ObjectId id)
        {
            return world.ObjectCache.StandardImitate<GameScript>(id, async (meta) =>
            {
                var p = new GameScript(world, meta, id);
                var code = await world.StorageManager.GetScriptCode(id);
                p.Code = code;
                return p;
            });
        }

        public string? Code { get; private set; }

        protected GameScript(IWorld world, ObjectMetadata meta, ObjectId? id = null) : base(world, GameObjectType.Script, meta, id) { }

        public override async Task<bool> Invalidate()
        {
            var result = await base.Invalidate();
            if (!result)
            {
                return false;
            }

            // TODO: Merge this with the Imitate meta callback
            var code = await _world.StorageManager.GetScriptCode(Id);
            this.Code = code;
            return true;
        }
    }
}
