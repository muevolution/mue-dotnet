#!worldscript who
from mue_engine import MueBinding  # type: ignore


def __mue_entry__(mue: MueBinding):
    playerList = mue.world.connected_players()
    if len(playerList) < 1:
        mue.log.warn("Somehow the server is empty!")
        return

    playerNames = map(lambda name: mue.world.get_name(name), playerList)
    mue.world.tell("Connected players: " + ", ".join(playerNames))
