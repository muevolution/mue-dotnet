using System;
using Mue.Scripting;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Scripting.Implementation
{
    public class MueScriptTypes
    {
        public MueScriptTypes(IWorld world, MueEngineExecutor executor)
        {
        }

        [MueExposedScriptMethod]
        public string FromId(string objectId)
        {
            var objId = new ObjectId(objectId);
            return objId.ObjectType.ToShortString();
        }
    }
}