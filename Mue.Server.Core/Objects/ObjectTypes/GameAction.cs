using System;
using System.Linq;
using System.Threading.Tasks;
using Mue.Server.Core.Models;
using Mue.Server.Core.System;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.Objects
{
    public record ActionMetadata : ObjectMetadata
    {
        public ObjectId? Target { get; init; }
    }

    public class GameAction : GameObject<ActionMetadata>
    {
        public static Task<GameAction> Create(IWorld world, string name, ObjectId creator, ObjectId? location = null)
        {
            if (name.StartsWith("$"))
            {
                throw new IllegalObjectNameException(name, GameObjectType.Action);
            }

            var p = new GameAction(world, new ActionMetadata
            {
                Name = name,
                Creator = creator,
                Parent = creator,
                Location = location,
            });

            return world.ObjectCache.StandardCreate(p);
        }

        public static Task<GameAction> Imitate(IWorld world, ObjectId id)
        {
            return world.ObjectCache.StandardImitate<GameAction, ActionMetadata>(id, (meta) => Task.FromResult(new GameAction(world, meta, id)));
        }

        protected GameAction(IWorld world, ActionMetadata meta, ObjectId? id = null) : base(world, GameObjectType.Action, meta, id) { }

        // reparent

        public ObjectId? Target { get { return Meta.Target; } }

        public Task<bool> SetTarget(ObjectId target)
        {
            if (!GameObjectConsts.AllActionTargets.Contains(target.ObjectType))
            {
                throw new InvalidGameObjectTargetException(target);
            }

            Meta = Meta with { Target = target };
            return _world.StorageManager.UpdateMeta(Id, Meta);
        }

        public bool MatchCommand(string command)
        {
            if (String.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            return Name.Split(";").Select(s => s.ToLower()).Contains(command);
        }
    }
}