using Velopack;
using WslTamer.App.Services;
using WslTamer.Core.Storage;

namespace WslTamer.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Must run first: handles install, update and uninstall hooks, then returns.
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => new StartupRegistration(AppPaths.ExecutablePath).SetEnabled(false))
            .Run();

        using var instance = SingleInstance.Acquire();
        if (!instance.IsFirst)
        {
            SingleInstance.ActivateFirstInstance();
            return 0;
        }

        var app = new App(args);
        app.InitializeComponent();
        instance.Activated += (_, _) => app.Dispatcher.BeginInvoke(app.ShowMainWindow);
        return app.Run();
    }
}
