#!worldscript whospecies;whospe;ws
from mue_engine import MueBinding  # type: ignore


def __mue_entry__(mue: MueBinding):
    thisRoom = mue.world.get_location(mue.script.this_player)

    playerList = mue.world.get_contents(thisRoom, mue.types.player)
    if len(playerList) < 1:
        mue.log.warn("Somehow this room was empty")
        return

    playerDetails = map(
        lambda p: {
            "details": mue.world.get_details(p),
            "gender": mue.world.get_prop(p, "gender") or "--",
            "species": mue.world.get_prop(p, "species") or "--",
        },
        playerList,
    )

    # table = mue.util.create_table(
    table = [
        ["Name", "Gender", "Species"],
        *map(lambda p: [p["details"].name, p["gender"], p["species"]], playerDetails),
    ]

    mue.world.tell_table(table, message="Players in room", hasHeader=True)
