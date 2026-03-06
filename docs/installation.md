# Installation

## Prerequisites
- .NET 10 SDK
- .NET MAUI workload
- Windows for output capture (loopback)

## Build and run
1. Install workloads:
   - `dotnet workload install maui`
2. Restore dependencies:
   - `dotnet restore`
3. Run the app:
   - `dotnet build`
   - `dotnet run --project MWG.EyesOfApollo.Desktop`

## Notes
- Output capture uses Windows loopback; other platforms currently expose placeholders.
