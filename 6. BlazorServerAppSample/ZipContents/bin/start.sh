#!/bin/sh

echo "Starting frontend plugin"
python3 bin/frontend.py @APPNAME@ &

cd $(dirname $0)

chmod +x ./@APPEXEC@

while true; do
	echo "Starting @APPNAME@"
    ./@APPEXEC@
done
