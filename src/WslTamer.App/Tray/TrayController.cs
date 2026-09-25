using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Wpf.Ui.Controls;
using WslTamer.App.Services;
using WslTamer.Core.Wsl;
using MenuItem = System.Windows.Controls.MenuItem;

namespace WslTamer.App.Tray;

/// <summary>The notification-area icon and its menu, rebuilt each time it opens.</summary>
public sealed class TrayController(AppController controller, IWslClient wsl, UserInteraction ui) : ITrayNotifier, IDisposable
{
    private TaskbarIcon? _icon;

    public event EventHandler? OpenRequested;

    public event EventHandler? ExitRequested;

    public void Create()
    {
        _icon = new TaskbarIcon
        {
            ToolTipText = "WSL Tamer",
            Icon = TrayIconRenderer.Render(TrayState.Stopped),
            MenuActivation = PopupActivationMode.RightClick,
            NoLeftClickDelay = true,
            ContextMenu = new ContextMenu(),
        };
        _icon.TrayLeftMouseUp += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        _icon.PreviewTrayContextMenuOpen += (_, _) => BuildMenu(_icon.ContextMenu);
        _icon.ForceCreate(enablesEfficiencyMode: false);

        controller.StateChanged += (_, _) => UpdateIcon();
        UpdateIcon();
    }

    public void ShowBalloon(string title, string message, Severity severity) =>
        _icon?.ShowNotification(title, message, severity switch
        {
            Severity.Warning => NotificationIcon.Warning,
            Severity.Error => NotificationIcon.Error,
            _ => NotificationIcon.Info,
        });

    public void Dispose() => _icon?.Dispose();

    private void UpdateIcon()
    {
        if (_icon is null)
        {
            return;
        }

        var status = controller.Status;
        var state = status.RestartPending ? TrayState.RestartPending
            : status.AnyRunning ? TrayState.Running
            : TrayState.Stopped;

        _icon.Icon = TrayIconRenderer.Render(state);
        _icon.ToolTipText = state switch
        {
            TrayState.RestartPending => "WSL Tamer — restart WSL to apply changes",
            TrayState.Running => $"WSL Tamer — running ({status.RunningDistros.Count} distribution{(status.RunningDistros.Count == 1 ? "" : "s")})",
            _ => "WSL Tamer — WSL is stopped",
        };
    }

    private void BuildMenu(ContextMenu menu)
    {
        menu.Items.Clear();
        var status = controller.Status;

        menu.Items.Add(new MenuItem
        {
            Header = status.AnyRunning ? $"WSL running · {status.RunningDistros.Count} active" : "WSL stopped",
            IsEnabled = false,
            FontWeight = FontWeights.SemiBold,
        });

        if (status.RestartPending)
        {
            menu.Items.Add(Item("Restart WSL to apply changes", SymbolRegular.ArrowClockwise24, () => controller.RestartWslAsync()));
        }

        if (controller.KeepAlivePaused)
        {
            menu.Items.Add(Item("Resume background distributions", SymbolRegular.Play24, () => controller.ResumeKeepAlive()));
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(BuildProfilesMenu());
        menu.Items.Add(BuildDistrosMenu());
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Reclaim memory", SymbolRegular.Broom24, controller.ReclaimMemoryAsync, enabled: status.AnyRunning));
        menu.Items.Add(Item("Shut down WSL", SymbolRegular.Power24, controller.ShutdownWslAsync, enabled: status.AnyRunning));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Open WSL Tamer", SymbolRegular.Window24, () => OpenRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(Item("Exit", SymbolRegular.Dismiss24, () => ExitRequested?.Invoke(this, EventArgs.Empty)));
    }

    private MenuItem BuildProfilesMenu()
    {
        var parent = new MenuItem { Header = "Profile", Icon = new SymbolIcon(SymbolRegular.Gauge24) };
        var active = controller.GetActiveProfile();
        foreach (var profile in controller.State.Config.Profiles)
        {
            var item = Item(profile.Name, null, () => controller.ApplyProfileAsync(profile));
            item.IsCheckable = true;
            item.IsChecked = active?.Id == profile.Id;
            parent.Items.Add(item);
        }

        if (parent.Items.Count == 0)
        {
            parent.Items.Add(new MenuItem { Header = "No profiles", IsEnabled = false });
        }

        return parent;
    }

    private MenuItem BuildDistrosMenu()
    {
        var parent = new MenuItem { Header = "Distributions", Icon = new SymbolIcon(SymbolRegular.WindowConsole20) };
        foreach (var distro in controller.GetDistributionsSnapshot())
        {
            var item = new MenuItem { Header = distro.IsRunning ? $"{distro.Name}  ●" : distro.Name };
            item.Items.Add(Item("Open terminal", SymbolRegular.WindowConsole20, () => ui.RunAsync($"Could not open {distro.Name}", () =>
            {
                wsl.OpenTerminal(distro.Name);
                return Task.CompletedTask;
            })));
            item.Items.Add(Item("Stop", SymbolRegular.Stop24, () => ui.RunAsync($"Could not stop {distro.Name}", async () =>
            {
                await wsl.TerminateAsync(distro.Name);
                await controller.RefreshStatusAsync();
            }), enabled: distro.IsRunning));

            var keep = Item("Keep running in background", null, () => controller.SetKeepAlive(distro.Name, !controller.IsKeptAlive(distro.Name)));
            keep.IsCheckable = true;
            keep.IsChecked = controller.IsKeptAlive(distro.Name);
            item.Items.Add(keep);
            parent.Items.Add(item);
        }

        if (parent.Items.Count == 0)
        {
            parent.Items.Add(new MenuItem { Header = "No distributions installed", IsEnabled = false });
        }

        return parent;
    }

    private static MenuItem Item(string header, SymbolRegular? icon, Func<Task> action, bool enabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled };
        if (icon is { } symbol)
        {
            item.Icon = new SymbolIcon(symbol);
        }

        item.Click += async (_, _) => await action();
        return item;
    }

    private static MenuItem Item(string header, SymbolRegular? icon, Action action, bool enabled = true) =>
        Item(header, icon, () =>
        {
            action();
            return Task.CompletedTask;
        }, enabled);
}
