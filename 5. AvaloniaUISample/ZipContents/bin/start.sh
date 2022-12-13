#!/bin/sh

cd $(dirname $0)

chmod +x ./@APPEXEC@

while true; do
	echo "Starting @APPNAME@"
    DISPLAY=:0 ./@APPEXEC@
done
