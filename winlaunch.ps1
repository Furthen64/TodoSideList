$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Configuration = if ($args.Count -gt 0) { $args[0] } else { "Debug" }

Set-Location $Root
dotnet run --project .\TodoSideList.App\TodoSideList.App.csproj -c $Configuration
