#!worldscript i;inv;inventory
from mue_engine import MueBinding  # type: ignore


def __mue_entry__(mue: MueBinding):
    outputLines = []

    # Get the contents of the player
    contents = mue.world.get_contents(mue.script.this_player)
    if len(contents) > 0:
        # Build the item list
        outputLines.append("You are carrying:")
        for itemId in contents:
            item = mue.world.get_details(itemId)
            if item.type not in (mue.types.room, mue.types.action):
                outputLines.append(" - " + item.name + " [" + itemId + "]")
    else:
        # :(
        outputLines.append("You aren't carrying anything.")

    # Tell the player
    mue.world.tell("\n".join(outputLines))
