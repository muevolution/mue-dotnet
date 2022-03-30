#!worldscript say
from mue_engine import MueBinding # type: ignore


def __mue_entry__(mue: MueBinding):
    sayMessage = mue.script.command.args
    formats = {
        "FirstPerson": 'You say, "{{message}}"',
        "ThirdPerson": '{{to_name speaker}} says, "{{message}}"',
    }
    content = {"message": sayMessage, "speaker": mue.script.this_player}

    thisRoom = mue.world.get_location(mue.script.this_player)
    mue.world.tell_extended(formats, content, thisRoom)
