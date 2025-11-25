# WSL Tamer UI Test Suite

Comprehensive automated UI testing for WSL Tamer using FlaUI (UI Automation framework).

## Overview

This test suite provides comprehensive coverage of the WSL Tamer user interface, including:

- **Settings Window Tests** - Navigation and window behavior
- **General Page Tests** - WSL management, memory reclaim, startup settings
- **Distributions Page Tests** - Distribution list, install/remove operations
- **Profiles Page Tests** - Profile management, settings configuration
- **Hardware Page Tests** - USB device management, disk mounting
- **About Page Tests** - Version info, update checks, licensing

## Prerequisites

- .NET 8.0 SDK
- Windows 10/11
- WSL Tamer built in Debug configuration

## Running Tests

### Run All Tests

```powershell
.\run-ui-tests.ps1
```

### Run Tests by Category

```powershell
# Settings window tests
.\run-ui-tests.ps1 "TestCategory=Settings"

# General page tests
.\run-ui-tests.ps1 "TestCategory=General"

# Distributions page tests
.\run-ui-tests.ps1 "TestCategory=Distributions"

# Profiles page tests
.\run-ui-tests.ps1 "TestCategory=Profiles"

# Hardware page tests
.\run-ui-tests.ps1 "TestCategory=Hardware"

# About page tests
.\run-ui-tests.ps1 "TestCategory=About"

# Layout tests (all pages)
.\run-ui-tests.ps1 "TestCategory=Layout"

# Theme tests
.\run-ui-tests.ps1 "TestCategory=Theme"

# Dialog tests
.\run-ui-tests.ps1 "TestCategory=Dialog"
```

### Run Specific Test

```powershell
dotnet test src\WslTamer.UITests\WslTamer.UITests.csproj --filter "Name~SettingsWindow_Opens_Successfully"
```

## Test Structure

```
WslTamer.UITests/
├── TestBase.cs                          # Base class with common functionality
├── Tests/
│   ├── SettingsWindowTests.cs          # Settings window and navigation (11 tests)
│   ├── GeneralPageTests.cs             # General page functionality (10 tests)
│   ├── DistributionsPageTests.cs       # Distribution management (10 tests)
│   ├── ProfilesPageTests.cs            # Profile configuration (14 tests)
│   ├── HardwarePageTests.cs            # Hardware management (12 tests)
│   └── AboutPageTests.cs               # About page and updates (13 tests)
└── WslTamer.UITests.csproj
```

**Total: 70 comprehensive UI tests**

## Features

### Automatic Screenshots on Failure

When a test fails, a screenshot is automatically captured and saved to:
```
src\WslTamer.UITests\bin\Debug\net8.0-windows\TestName_Timestamp.png
```

### Test Isolation

Each test:
- Starts with a fresh application instance
- Cleans up all processes after completion
- Runs independently without affecting other tests

### Wait Strategies

Tests use intelligent waiting for:
- Application startup
- Window loading
- Async data loading
- User interactions

## Test Categories

### Settings
Tests for the main settings window structure and navigation.

### General
Tests for WSL status, memory management, and general operations.

### Distributions
Tests for distribution listing, installation, and management.

### Profiles
Tests for profile creation, editing, and configuration.

### Hardware
Tests for USB device and disk management.

### About
Tests for version information, updates, and licensing.

### Layout
Tests for UI layout consistency across all pages.

### Theme
Tests for dark/light theme support.

### Dialog
Tests for dialog windows (install, settings, etc.).

## Adding New Tests

1. Create a new test class in `Tests/` folder
2. Inherit from `TestBase`
3. Use `[TestFixture]` attribute
4. Add `[Test]` and `[Category]` attributes to test methods
5. Follow the existing naming convention: `PageName_Action_ExpectedResult`

Example:

```csharp
namespace WslTamer.UITests.Tests;

[TestFixture]
public class MyNewTests : TestBase
{
    [Test]
    [Category("MyCategory")]
    public void MyPage_DoSomething_ShouldSucceed()
    {
        // Arrange
        var window = GetSettingsWindow();
        
        // Act
        // ... perform actions
        
        // Assert
        Assert.That(result, Is.Not.Null);
    }
}
```

## Troubleshooting

### Tests Fail to Start Application

**Issue**: Application not found at expected path

**Solution**: Build the solution first:
```powershell
dotnet build -c Debug
```

### Tests Hang or Timeout

**Issue**: Application doesn't respond or UI elements not found

**Solution**: 
- Check if WSL Tamer is already running and close it
- Increase wait times in tests if needed
- Check application logs for errors

### Screenshots Not Generated

**Issue**: No screenshots after test failure

**Solution**: Ensure the test binary output directory is writable.

### Inconsistent Results

**Issue**: Tests pass/fail randomly

**Solution**:
- Ensure system is not under heavy load
- Close other applications that might interfere
- Check if WSL itself is functioning properly

## CI/CD Integration

To run tests in CI/CD pipelines:

```yaml
- name: Run UI Tests
  run: |
    dotnet build -c Debug
    dotnet test src/WslTamer.UITests/WslTamer.UITests.csproj --logger "trx;LogFileName=test-results.trx"
```

## Known Limitations

1. **Tray Icon Interaction**: Some tests may not fully interact with the system tray icon due to Windows security restrictions.

2. **Admin Rights**: Tests requiring admin privileges (like USB device attachment) may need elevation.

3. **WSL Dependency**: Tests that interact with actual WSL distributions require WSL to be installed and functional.

4. **Timing Sensitivity**: Some tests may be sensitive to system performance and timing.

## Future Enhancements

- [ ] Add integration tests with mock WSL backend
- [ ] Add performance benchmarks
- [ ] Add accessibility tests
- [ ] Add tests for keyboard navigation
- [ ] Add tests for dialog windows (Clone, Import, Move, etc.)
- [ ] Add tests for context menus
- [ ] Add tests for error handling scenarios
- [ ] Add visual regression testing
- [ ] Add load testing for large distribution lists

## Contributing

When adding new features to WSL Tamer, please:

1. Add corresponding UI tests
2. Run the full test suite before submitting PR
3. Update this README if adding new test categories
4. Ensure tests are deterministic and don't require manual interaction

## License

Same as WSL Tamer - MIT License
