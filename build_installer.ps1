$ErrorActionPreference = "Stop"

Write-Host "Cleaning previous builds..."
dotnet clean
if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }
if (Test-Path "src\WslTamer.Installer\bin") { Remove-Item "src\WslTamer.Installer\bin" -Recurse -Force }
if (Test-Path "src\WslTamer.Installer\obj") { Remove-Item "src\WslTamer.Installer\obj" -Recurse -Force }

Write-Host "Publishing WslTamer.UI..."
dotnet publish src\WslTamer.UI\WslTamer.UI.csproj -c Release -o publish /p:Version=1.1.1

Write-Host "Building Installer..."
dotnet build src\WslTamer.Installer\WslTamer.Installer.wixproj -c Release

Write-Host "Done! Installer should be in src\WslTamer.Installer\bin\Release\WslTamer.msi"
