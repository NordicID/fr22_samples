## RPCDemo

Simple dotnet backend application using FR22 RPC API to read firmware version.

Application uses .net framework 4.8 mono runtime, installed from FR22 app center.

### Usage

- Application package must be installed through FR22 webui application interface.
- When application is running, it prints firmware version to log

### Generating FR22 application package

#### Visual Studio 2022
- Open solution with Visual Studio 2022
- After successful build, package 'RPCDemo_1.0.0.0-app.zip' is generated in the solution folder.

### Publish integration
See project Post-build event.
