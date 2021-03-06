using Mue.Server.Core.Objects;

namespace Mue.Server.Core.Scripting;

public interface IScriptManager
{
    Task RunScript(GameScript script, GamePlayer runBy, LocalCommand command);
}
