# Building CryptoFear on Windows

## Prerequisites

1. **.NET 8 SDK**  
   - [Download .NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

2. **.NET MAUI workload for Windows** (required once per machine)

   Run in this folder (Command Prompt or PowerShell as Administrator if needed):

   ```bat
   install-workloads.bat
   ```

   Or manually:

   ```bat
   dotnet workload install maui-windows
   ```

   If the build still fails with an error asking for **maui-tizen**, that’s a known SDK bug when targeting only Windows. Install what the SDK asks for:

   ```bat
   dotnet workload restore
   ```

   - [.NET MAUI installation (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation)  
   - [Install .NET workloads (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-install)

3. **Windows 10 SDK** (usually installed with Visual Studio or the workload)  
   - Build target: Windows 10.0.19041.0

## Build

```bat
build.bat
```

Or:

```bat
dotnet build CryptoFear.csproj -f net8.0-windows10.0.19041.0 -c Release
```

## Run

```bat
run.bat
```

Or:

```bat
dotnet run --project CryptoFear.csproj -f net8.0-windows10.0.19041.0 -c Release
```

## Optional: Visual Studio 2022

You can also open the project in Visual Studio 2022 and build/run from there. Install the **.NET Multi-platform App UI development** workload:

- [Download Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)  
- In the installer: **Workloads** → **.NET Multi-platform App UI development**
