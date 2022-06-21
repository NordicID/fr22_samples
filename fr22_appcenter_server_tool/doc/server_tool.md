# App Center Server Tool

App Center server tool ([app-center.py](../app-center.py)) builds the repository (repo/ and repo.zip) according to given packages and meta data for those.

The built repository can be copied to any web server to host App Center for FR22 devices.

See [file structure](server_tool_file_structure.md)

## Building repository
- Put the application packages into repo/app/
- Add additional application information into meta/app/
- Put the system plugin packages into repo/sys/
- Add additional system plugin information into meta/sys/
- Put the firmware packages into repo/fw/
- Add firmware information into meta/fw/
- Run [app-center.py](../app-center.py)
- Either copy repo/ or unzip repo.zip to the web server
