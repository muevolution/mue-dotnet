# This is the typing stubs for the C# ScriptIntegration

from typing import Union

class ScriptCommand(object):
    command: str
    args: str
    params: dict[str, str]

class Script(object):
    this_script: str
    this_player: str
    command: ScriptCommand

class MessageFormats(object):
    FirstPerson: str
    ThirdPerson: str

class ObjectMetadata(object):
    name: str
    creator: str
    parent: str
    location: str
    type: str

PropValue = Union[str, int, list[Union[str, int]]]

class World(object):
    def tell(
        self, message: str, target: str = None, meta: dict[str, str] = None
    ) -> None: ...
    def tell_extended(
        self,
        extended_format: dict[str, str],
        extended_content: dict[str, str],
        target: str = None,
        meta: dict[str, str] = None,
    ) -> None: ...
    def tell_table(
        self,
        table: list[list[str]],
        message: str = None,
        hasHeader: bool = False,
        target: str = None,
        meta: dict[str, str] = None,
    ) -> None: ...
    def connected_players(self) -> list[str]: ...
    def get_player_id_from_name(self, player_name: str) -> str: ...
    def get_name(self, object_id: str) -> str: ...
    def get_parent(self, object_id: str) -> str: ...
    def get_location(self, object_id: str) -> str: ...
    def find(self, target: str) -> str: ...
    def get_details(self, object_id: str) -> ObjectMetadata: ...
    def get_prop(self, object_id: str, path: str) -> PropValue: ...
    def get_props(self, object_id: str) -> dict[str, PropValue]: ...
    def get_contents(self, object_id: str, of_type: str = None) -> list[str]: ...

class MueLogger(object):
    def log(self, level: str, message: str, *args): ...
    def debug(self, message: str, *args): ...
    def info(self, message: str, *args): ...
    def warn(self, message: str, *args): ...
    def error(self, message: str, *args): ...

class ObjectTypes(object):
    action = "a"
    item = "i"
    player = "p"
    room = "r"
    script = "s"
    def from_id(self, object_id: str) -> str: ...

class TableResult(object):
    text: str
    raw: list[list[str]]

class Utils(object):
    def create_table(self, *kwargs) -> TableResult: ...

class MueBinding(object):
    script: Script
    world: World
    log: MueLogger
    types: ObjectTypes
    util: Utils
