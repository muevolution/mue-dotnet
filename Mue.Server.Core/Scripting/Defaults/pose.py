#!worldscript pose
from mue_engine import MueBinding  # type: ignore


def __mue_entry__(mue: MueBinding):
    sayMessage = mue.script.command.args
    formats = {
        "FirstPerson": "{{to_name actor}} {{action}}",
        "ThirdPerson": "{{to_name actor}} {{action}}",
    }
    content = {"action": sayMessage, "actor": mue.script.this_player}

    thisRoom = mue.world.get_location(mue.script.this_player)
    mue.world.tell_extended(formats, content, thisRoom)
