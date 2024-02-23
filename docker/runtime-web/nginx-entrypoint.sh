#!/bin/sh

set -e

# Create the runtime configuration JSON file

if [ $USE_PROXY == "true" ]; then
    # Using the internal proxy
    APP_SERVER_URL="/mueclient"
else
    # Not using the internal proxy, a backend server URL is required
    if [ -x $BACKEND_SERVER_URL ]; then
        echo "No backend URL set, cannot continue"
        exit 1
    fi

    APP_SERVER_URL=$BACKEND_SERVER_URL/mueclient
fi

echo "{\"hubUrl\":\"${APP_SERVER_URL}\"}" > /usr/share/nginx/html/server.json

echo "Runtime configuration output"
cat /usr/share/nginx/html/server.json
echo
