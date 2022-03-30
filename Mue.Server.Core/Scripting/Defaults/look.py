#!worldscript l;look;lookat
from mue_engine import MueBinding  # type: ignore


def __mue_entry__(mue: MueBinding):
    # Parse commands
    command = mue.script.command

    lookTarget = None
    if command.params and "target" in command.params:
        lookTarget = command.params["target"]
    elif command.args:
        lookTarget = command.args

    lookObj = None
    if not lookTarget:
        # Look at the room
        lookObj = mue.world.get_location(mue.script.this_player)
    else:
        # Find the named target
        lookObj = mue.world.find(lookTarget)

    if not lookObj:
        mue.world.tell("I couldn't find that.")
        return

    # Construct the output
    outputLines = []

    # Get the name if it's supposed to be shown
    if mue.types.from_id(lookObj) == mue.types.room:
        roomDetails = mue.world.get_details(lookObj)
        outputLines.append(roomDetails.name)

    # Add the description
    desc = mue.world.get_prop(lookObj, "description")
    if not desc:
        outputLines.append("You see nothing special.")
    else:
        outputLines.append(desc)

    # Collect the visible contents
    contents = mue.world.get_contents(lookObj)
    if len(contents) > 0:
        outputLines.append("---------")
        outputLines.append("Contents:")
        for itemId in contents:
            item = mue.world.get_details(itemId)
            if item.type not in (mue.types.room, mue.types.action):
                outputLines.append(" - " + item.name + " [" + itemId + "]")

    # Finally, send to the player!
    mue.world.tell("\n".join(outputLines))
