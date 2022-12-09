## AvaloniaUISample

Simple dotnet UI application using AvaloniaUI running inside FR22.

Application is packaged with net6.0 self-contained linux-arm runtime.

### Usage

- Application package must be installed through FR22 webui application interface.
- When application is running, it is shown in FR22 HDMI output
- In case no display connected to FR22 HDMI output, you can install 'Web VNC' from FR22 app center and open remote desktop to FR22 display (Remote Tools -> Remote Desktop).

### Generating FR22 application package

#### Visual Studio 2022
- Open solution with Visual Studio 2022
- Right mouse click project name in solution explorer and select 'publish'
- Click publish button and AvaloniaUISample_1.0.1-app.zip is generated in solution folder

#### dotnet command line
`dotnet publish -r linux-arm --sc -c Release_FR22 -o "FR22PublishOutput" -p:PublishProfile=FR22_App`

After package is generated 'FR22PublishOutput' folder can be removed.

### Publish integration
See GenerateFr22Zip at the end of the project file:
`<Target Condition="'$(Configuration)' == 'Release_FR22'" Name="GenerateFr22Zip" AfterTargets="Publish">`

This will execute fr22_vs_appziptool\build.ps1 powershell script after publish is made for 'Release_FR22' target.
