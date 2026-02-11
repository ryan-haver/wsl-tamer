# Quick test runner for a single test
# Run as Administrator

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Please run PowerShell as Administrator" -ForegroundColor Red
    exit 1
}

Write-Host "Cleaning up..." -ForegroundColor Yellow
Get-Process -Name "WslTamer.UI" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Running diagnostic test..." -ForegroundColor Green
dotnet test src\WslTamer.UITests\WslTamer.UITests.csproj --filter "TestCategory=Diagnostic" --logger "console;verbosity=detailed"

Write-Host ""
Write-Host "Cleaning up..." -ForegroundColor Yellow
Get-Process -Name "WslTamer.UI" -ErrorAction SilentlyContinue | Stop-Process -Force
