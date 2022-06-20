# App Center Server Tool file structure

## meta/

Input directory for meta data in JSON format.

### meta/app/*ApplicationName*.json

Optional application description and display name.
*ApplicationName* is the name of the application as specified in meta/manifest.json in application package zip file.\
[*ApplicationName*.json format](plugin.json.md)

### meta/app/*ApplicationName*/*ApplicationVersion*.json

Optional information about application version.
*ApplicationVersion* is the version of the application as specified in meta/manifest.json in application package zip file.\
[*ApplicationVersion*.json format](plugin_version.json.md)


### meta/sys/*PluginName*.json

Optional system plugin description and display name.
*PluginName* is the name of the system plugin as specified in meta/manifest.json in system plugin package zip file.\
[*PluginName*.json format](plugin.json.md)

### meta/app/*PluginName*/*PluginVersion*.json

Optional information about system plugin version.
*PluginVersion* is the version of the system plugin as specified in meta/manifest.json in system plugin package zip file.\
[*PluginVersion*.json format](plugin_version.json.md)

### meta/fw/*FirmwareName*.json

Mandatory firmware release description file for each firmware release. *FirmwareName* can be chosen freely.\
[*FirmwareName*.json format](firmware.json.md)

## repo/

Repository directory which can be copied to server after build process.

### repo/index.html

By default an empty file to prevent web servers directly showing directory structure.

### repo/index.json

Output of the server tool describing the repository content.

### repo/app/

Directory for the signed application package zip files. Subdirectories are allowed to be used.

### repo/sys/

Directory for the signed system plugin package zip files. Subdirectories are allowed to be used.

### repo/fw/

Directory for the firmware package zip files. Subdirectories as specified in [meta/fw/*FirmwareName*.json](firmware.json.md)

### repo.zip

Output of the server building process. Contains the relevant parts of the repo/ content packaged in zip format.

### index.json

repo/index.json formatted to more human readable form.\
[index.json format](index.json.md)
