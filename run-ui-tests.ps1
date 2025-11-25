# Run UI Tests for WSL Tamer

Write-Host "WSL Tamer UI Test Runner" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

# Check if the application is built
$appPath = ".\src\WslTamer.UI\bin\Debug\net8.0-windows\WslTamer.UI.exe"
if (-not (Test-Path $appPath)) {
    Write-Host "Application not found at: $appPath" -ForegroundColor Red
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build -c Debug
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed! Please fix build errors before running tests." -ForegroundColor Red
        exit 1
    }
}

# Kill any existing instances
Write-Host "Cleaning up existing WSL Tamer instances..." -ForegroundColor Yellow
Get-Process -Name "WslTamer.UI" -ErrorAction SilentlyContinue | Stop-Process -Force

# Run the tests
Write-Host ""
Write-Host "Running UI tests..." -ForegroundColor Green
Write-Host ""

# You can filter tests by category using --filter
# Examples:
#   --filter "TestCategory=Settings"
#   --filter "TestCategory=General"
#   --filter "TestCategory=Distributions"
#   --filter "TestCategory=Profiles"
#   --filter "TestCategory=Hardware"
#   --filter "TestCategory=About"
#   --filter "TestCategory=Layout"
#   --filter "TestCategory=Theme"

$filter = $args[0]

if ($filter) {
    Write-Host "Running filtered tests: $filter" -ForegroundColor Cyan
    dotnet test src\WslTamer.UITests\WslTamer.UITests.csproj -c Debug --filter $filter -l "console;verbosity=normal"
} else {
    dotnet test src\WslTamer.UITests\WslTamer.UITests.csproj -c Debug -l "console;verbosity=normal"
}

$exitCode = $LASTEXITCODE

# Cleanup
Write-Host ""
Write-Host "Cleaning up..." -ForegroundColor Yellow
Get-Process -Name "WslTamer.UI" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "Tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Some tests failed. Check the output above for details." -ForegroundColor Red
}

Write-Host ""
Write-Host "Screenshots from failed tests can be found in:" -ForegroundColor Cyan
Write-Host "  src\WslTamer.UITests\bin\Debug\net8.0-windows\" -ForegroundColor White

exit $exitCode
