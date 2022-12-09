write-host "Generating FR22 application package"

$ScriptFolder=$PSScriptRoot
$ZipContents=$args[0]
$AppName=$args[1]
$AppTarget=$args[2]
$PubFolder=$args[3]
$OutFolder=""
$AppVersion="0.0.0"
$AppVersion = (dir "$AppTarget").VersionInfo.ProductVersion

if ((Get-Item "$AppTarget").Extension -eq ".exe") {
	$AppExec=(Get-Item "$AppTarget").Name
} else {
	$AppExec=(Get-Item "$AppTarget").Basename
}

if ($args.length -gt 4) {
	$OutFolder=$args[4]
} else {
	$OutFolder=$ScriptFolder
}
$TmpFolder="$OutFolder\pkgtmp_$AppName"

# Cleanup tmp folder just in case
rm -r -force "$Tmpfolder" 2>&1> $null

# Create FR22 package folders
mkdir -p "$OutFolder" 2>&1> $null
mkdir -p "$TmpFolder\bin" 2>&1> $null
mkdir -p "$TmpFolder\data" 2>&1> $null
mkdir -p "$TmpFolder\meta" 2>&1> $null

write-host "ZipContents folder '$ZipContents'"
write-host "Script folder '$ScriptFolder'"
write-host "Publish folder '$PubFolder'"
write-host "AppTarget '$AppTarget'"
write-host "AppExec '$AppExec'"
write-host "AppName '$AppName'"
write-host "AppVersion '$AppVersion'"
write-host "OutFolder '$OutFolder'"

# Copy all files from 'ZipContents' to FR22 package folder
Copy-Item -Path "$ZipContents\*" -Destination "$Tmpfolder" -Recurse 2> $null
# Copy all published files to FR22 package bin folder
Copy-Item -Path "$PubFolder\*" -Destination "$Tmpfolder\bin" -Recurse 2> $null

if (Get-Item -Path "$TmpFolder\bin\start.sh" -ErrorAction Ignore) {
	# Replace @APPNAME@ and @APPEXEC@ in package start.sh
	((Get-Content -path "$TmpFolder\bin\start.sh" -Raw) -replace '@APPNAME@', "$AppName") | Set-Content -Path "$TmpFolder\bin\start.sh"
	((Get-Content -path "$TmpFolder\bin\start.sh" -Raw) -replace '@APPEXEC@', "$AppExec") | Set-Content -Path "$TmpFolder\bin\start.sh"
	write-host "start.sh content"
	Get-Content -path "$TmpFolder\bin\start.sh"
}

# Replace @APPNAME@ and @APPVERSION@ in package manifest.json
((Get-Content -path "$TmpFolder\meta\manifest.json" -Raw) -replace '@APPNAME@', "$AppName") | Set-Content -Path "$TmpFolder\meta\manifest.json"
((Get-Content -path "$TmpFolder\meta\manifest.json" -Raw) -replace '@APPVERSION@', "$AppVersion") | Set-Content -Path "$TmpFolder\meta\manifest.json"
((Get-Content -path "$TmpFolder\meta\manifest.json" -Raw) -replace '@APPEXEC@', "$AppExec") | Set-Content -Path "$TmpFolder\meta\manifest.json"
write-host "manifest.json content"
Get-Content -path "$TmpFolder\meta\manifest.json"

# Sign FR22 package folder and generate app zip
& "$ScriptFolder\..\fr22_appsigntool\fr_appsigntool.exe" $TmpFolder

# Cleanup tmp folder
rm -r -force "$Tmpfolder"

write-host "Done"
