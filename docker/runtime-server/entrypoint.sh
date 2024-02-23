#!/bin/bash

# Custom entrypoint to allow calling the tools executable

if [ "$1" == "help" ] || [ "$1" == "--help" ]; then
    echo "Usage: /entrypoint.sh <server|tools>"
    exit 0
elif [ "$1" == "tools" ]; then
    cd /app/Tools
    ./Mue.Server.Tools ${@:2}
elif [ "$1" == "server" ] || [ -z $1 ]; then
    cd /app/Server
    dotnet Mue.Server.dll ${@:2}
else
    echo "Unknown startup command $1"
    exit 1
fi
