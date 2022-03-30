using System;
using System.Reflection;
using Moq;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Tests
{
    public static class GameObjectMocker
    {
        public static Mock<IGameObjectMock> CreateMock<T>(ObjectId id) where T : class, IGameObject
        {
            var gameObj = new Mock<IGameObjectMock>();

            gameObj.SetupGet(s => s.Id).Returns(id);
            gameObj.SetupGet(s => s.ObjectType).Returns(id.ObjectType);

            gameObj.Setup(s => s.GetType()).Returns(typeof(T));

            return gameObj;
        }

        public static GameAction CreateRealAction(IWorld world, ObjectId id, string name, ObjectId creator = null, ObjectId location = null) => CreateReal<GameAction, ActionMetadata>(world, id, name, creator, location);
        public static GameItem CreateRealItem(IWorld world, ObjectId id, string name, ObjectId creator = null, ObjectId location = null) => CreateReal<GameItem>(world, id, name, creator, location);
        public static GamePlayer CreateRealPlayer(IWorld world, ObjectId id, string name, ObjectId creator = null, ObjectId location = null) => CreateReal<GamePlayer, PlayerMetadata>(world, id, name, creator, location);
        public static GameRoom CreateRealRoom(IWorld world, ObjectId id, string name, ObjectId creator = null, ObjectId location = null) => CreateReal<GameRoom>(world, id, name, creator, location);
        public static GameScript CreateRealScript(IWorld world, ObjectId id, string name, ObjectId creator = null, ObjectId location = null) => CreateReal<GameScript>(world, id, name, creator, location);

        private static T CreateReal<T>(IWorld world, ObjectId id, string name, ObjectId creator = null, ObjectId location = null) where T : class, IGameObject<ObjectMetadata> => CreateReal<T, ObjectMetadata>(world, id, name, creator, location);
        private static T CreateReal<T, MD>(IWorld world, ObjectId id, string name, ObjectId creator = null, ObjectId location = null) where T : class, IGameObject<MD> where MD : ObjectMetadata, new()
        {
            var meta = new MD { Name = name, Creator = creator, Location = location };
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                null,
                new object[] { world, meta, id },
                null
            );
        }

        public static void SetupMeta(this Mock<IGameObjectMock> mock, ObjectMetadata om)
        {
            // Both of these should return the same thing
            mock.Setup(s => s.Meta).Returns(om);
            mock.Setup(s => s.MetaBasic).Returns(om);
        }
    }

    public interface IGameObjectMock : IGameObject<ObjectMetadata>
    {
        Type GetType();
    }

    public class GameObjectMock : GameObject
    {
        public GameObjectMock(IWorld world, GameObjectType objType, ObjectMetadata meta, ObjectId id = null) : base(world, objType, meta, id) { }
    }
}