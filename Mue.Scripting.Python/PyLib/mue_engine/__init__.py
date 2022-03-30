# This is a wrapper around the C# ScriptIntegration


class ScriptCommand(object):
    def __init__(self, binding):
        self.command = binding.Get("Command")
        self.args = binding.Get("Args")
        self.params = binding.Get("Params")


class Script(object):
    def __init__(self, binding):
        self.this_script = binding.ThisScript
        self.this_player = binding.ThisPlayer
        self.command = ScriptCommand(binding.Command)


class World(object):
    def __init__(self, binding):
        self.binding = binding

    def tell(self, message, target=None, meta=None) -> None:
        return self.binding.TellShort(message, target, meta)

    def tell_extended(
        self,
        extended_format,
        extended_content,
        target=None,
        meta=None,
    ) -> None:
        return self.binding.TellExtended(
            extended_format, extended_content, target, meta
        )

    def tell_table(
        self, table, message=None, hasHeader=False, target=None, meta=None
    ) -> None:
        return self.binding.TellTable(table, message, hasHeader, target, meta)

    def connected_players(self):
        return list(self.binding.GetConnectedPlayers())

    def get_player_id_from_name(self, player_name):
        return self.binding.GetPlayerIdFromName(player_name)

    def get_name(self, object_id):
        return self.binding.GetName(object_id)

    def get_parent(self, object_id):
        return self.binding.GetParent(object_id)

    def get_location(self, object_id):
        return self.binding.GetLocation(object_id)

    def find(self, target):
        return self.binding.Find(target)

    def get_details(self, object_id):
        return self.binding.GetDetails(object_id)

    def get_prop(self, object_id, path):
        return self.binding.GetProp(object_id, path)

    def get_props(self, object_id):
        return self.binding.GetProps(object_id)

    def get_contents(self, object_id, of_type=None):
        return list(self.binding.GetContents(object_id, of_type))


class MueLogger(object):
    def __init__(self, binding):
        self.binding = binding

    def log(self, level, message, *args):
        arglist = None
        if args:
            arglist = list(args)

        return self.binding.Log(level, message, arglist)

    def debug(self, message, *args):
        return self.log("debug", message, *args)

    def info(self, message, *args):
        return self.log("info", message, *args)

    def warn(self, message, *args):
        return self.log("warn", message, *args)

    def error(self, message, *args):
        return self.log("error", message, *args)


class ObjectTypes(object):
    action = "a"
    item = "i"
    player = "p"
    room = "r"
    script = "s"

    def __init__(self, binding):
        self.binding = binding

    def from_id(self, object_id):
        return self.binding.FromId(object_id)


class Utils(object):
    def __init__(self, binding):
        self.binding = binding

    # def create_table(self, *args):
    #    toc = []
    #    for r in args:
    #        toc.append(list(r))
    #
    #    return self.binding.CreateTable(toc)


class MueBinding(object):
    def __init__(self, binding):
        if binding.Has("Callback"):
            self.callback = binding.Callback
        if binding.Has("Test"):
            self.test = binding.Test
        if binding.Has("LimitedImplementation"):
            return

        self.script = Script(binding.Script)
        self.world = World(binding.World)
        self.log = MueLogger(binding.Logger)
        self.types = ObjectTypes(binding.Types)
        self.util = Utils(binding.Utils)
