using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WslTamer.App.Services;
using WslTamer.App.Views.Dialogs;
using WslTamer.Core.Config;
using WslTamer.Core.Disks;
using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.App.ViewModels;

public sealed partial class DistroItemViewModel(WslDistribution distribution, VhdInfo? disk, bool keepAlive) : ObservableObject
{
    public WslDistribution Distribution { get; } = distribution;

    public string Name => Distribution.Name;

    public bool IsDefault => Distribution.IsDefault;

    public bool IsRunning => Distribution.IsRunning;

    public bool IsWsl2 => Distribution.Version == 2;

    public string Details => Distribution.DisplayVersion;

    public VhdInfo? Disk { get; } = disk;

    public bool IsSparse => Disk?.IsSparse == true;

    public string? DiskText => Disk is null
        ? null
        : Disk.IsSparse
            ? $"{WslValues.FormatBytes(Disk.AllocatedBytes)} on disk · sparse"
            : $"{WslValues.FormatBytes(Disk.AllocatedBytes)} on disk";

    [ObservableProperty]
    private bool _keepAlive = keepAlive;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyText;
}

public sealed partial class DistributionsViewModel(
    IWslClient wsl,
    IVhdService vhd,
    AppController controller,
    UserInteraction ui) : PageViewModel
{
    public ObservableCollection<DistroItemViewModel> Distributions { get; } = [];

    [ObservableProperty]
    private bool _isEmpty;

    public override Task OnNavigatedToAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await ui.RunAsync("Could not list distributions", async () =>
        {
            var list = await wsl.GetDistributionsAsync();
            var items = await Task.Run(() => list
                .Select(d => new DistroItemViewModel(d, vhd.GetInfo(d), controller.IsKeptAlive(d.Name)))
                .ToList());

            Distributions.Clear();
            foreach (var item in items)
            {
                Distributions.Add(item);
            }

            IsEmpty = Distributions.Count == 0;
        });
    }

    [RelayCommand]
    private Task OpenTerminalAsync(DistroItemViewModel item) => ui.RunAsync($"Could not open {item.Name}", async () =>
    {
        wsl.OpenTerminal(item.Name);
        await Task.Delay(1500);
        await RefreshAsync();
    });

    [RelayCommand]
    private Task StopAsync(DistroItemViewModel item) => Work(item, "Stopping…", $"Could not stop {item.Name}", async () =>
    {
        if (item.KeepAlive)
        {
            controller.SetKeepAlive(item.Name, false);
        }

        await wsl.TerminateAsync(item.Name);
    });

    [RelayCommand]
    private Task SetDefaultAsync(DistroItemViewModel item) =>
        Work(item, "Updating…", $"Could not make {item.Name} the default", () => wsl.SetDefaultAsync(item.Name));

    [RelayCommand]
    private async Task ToggleKeepAliveAsync(DistroItemViewModel item)
    {
        controller.SetKeepAlive(item.Name, !controller.IsKeptAlive(item.Name));
        ui.Notify(
            item.Name,
            controller.IsKeptAlive(item.Name)
                ? "Will keep running in the background while WSL Tamer is open."
                : "No longer kept running. WSL stops it when idle.");
        await Task.Delay(1500);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task EditConfigAsync(DistroItemViewModel item)
    {
        var window = new DistroConfigWindow(item.Name, wsl, ui) { Owner = Application.Current.MainWindow };
        window.ShowDialog();
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CloneAsync(DistroItemViewModel item)
    {
        var form = new FormDialog($"Clone {item.Name}", "Clone")
            .Text("Creates an independent copy, including installed packages and files. The source keeps running.")
            .Field("name", "New name", $"{item.Name}-copy")
            .FolderField("location", "Install location", DefaultLocation($"{item.Name}-copy"))
            .Validate(f => DistroNames.IsValid(f["name"]) ? null : "Use letters, numbers, '.', '_' or '-' for the name.")
            .Validate(f => Distributions.Any(d => d.Name.Equals(f["name"], StringComparison.OrdinalIgnoreCase)) ? "A distribution with that name exists." : null)
            .Validate(f => Path.IsPathFullyQualified(f["location"]) ? null : "Choose a folder for the new distribution.");

        if (await form.ShowAsync(ui))
        {
            await Work(item, "Cloning… this can take several minutes", $"Could not clone {item.Name}",
                () => wsl.CloneAsync(item.Name, form["name"], form["location"]),
                success: $"{form["name"]} is ready.");
        }
    }

    [RelayCommand]
    private async Task MoveAsync(DistroItemViewModel item)
    {
        var dialog = new OpenFolderDialog { Title = $"Move {item.Name} to…" };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (await ui.ConfirmAsync($"Move {item.Name}?", $"{item.Name} will stop and its disk will be moved to:\n{dialog.FolderName}", "Move"))
        {
            await Work(item, "Moving…", $"Could not move {item.Name}", () => wsl.MoveAsync(item.Name, dialog.FolderName), success: $"{item.Name} moved.");
        }
    }

    [RelayCommand]
    private async Task ExportAsync(DistroItemViewModel item)
    {
        var dialog = new SaveFileDialog
        {
            Title = $"Export {item.Name}",
            FileName = $"{item.Name}-{DateTime.Now:yyyy-MM-dd}",
            Filter = "Compressed archive (*.tar.gz)|*.tar.gz|Archive (*.tar)|*.tar|Compressed archive, smaller (*.tar.xz)|*.tar.xz|Virtual disk (*.vhdx)|*.vhdx",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var format = dialog.FilterIndex switch
        {
            2 => ExportFormat.Tar,
            3 => ExportFormat.TarXz,
            4 => ExportFormat.Vhd,
            _ => ExportFormat.TarGz,
        };

        await Work(item, "Exporting…", $"Could not export {item.Name}", () => wsl.ExportAsync(item.Name, dialog.FileName, format),
            success: $"Saved to {dialog.FileName}");
    }

    [RelayCommand]
    private async Task CompactAsync(DistroItemViewModel item)
    {
        if (!await ui.ConfirmAsync(
                $"Compact {item.Name}'s disk?",
                "WSL will shut down (all distributions stop), then Windows compacts the virtual disk to release unused space. You'll be asked for administrator permission.",
                "Shut down and compact"))
        {
            return;
        }

        CompactResult? result = null;
        await Work(item, "Compacting…", $"Could not compact {item.Name}", async () => result = await vhd.CompactAsync(item.Distribution));
        if (result is null)
        {
            return;
        }

        if (result.Outcome == ElevatedOutcome.Succeeded)
        {
            ui.Notify("Disk compacted", $"Reclaimed {WslValues.FormatBytes(result.BytesReclaimed)} from {item.Name}.", Severity.Success);
        }
        else if (result.Outcome == ElevatedOutcome.Failed)
        {
            ui.Notify("Compaction failed", $"Windows could not compact the disk. Make sure nothing else (such as Docker Desktop) is using {item.Name}.", Severity.Error);
        }
    }

    [RelayCommand]
    private async Task MakeSparseAsync(DistroItemViewModel item)
    {
        if (!await ui.ConfirmAsync(
                $"Use a sparse disk for {item.Name}?",
                $"A sparse disk shrinks automatically when files are deleted inside {item.Name}, so you rarely need to compact it. {item.Name} will stop while this is changed.",
                "Make sparse"))
        {
            return;
        }

        await Work(item, "Updating disk…", $"Could not change {item.Name}'s disk", async () =>
        {
            try
            {
                await wsl.SetSparseAsync(item.Name, true);
            }
            catch (SparseRequiresConsentException)
            {
                bool force = await ui.ConfirmAsync(
                    "WSL advises caution",
                    "This WSL version has sparse disks turned off by default because of a reported risk of data corruption. Only continue if you have a backup (use Export first).",
                    "Continue anyway",
                    destructive: true);
                if (force)
                {
                    await wsl.SetSparseAsync(item.Name, true, allowUnsafe: true);
                }
            }
        });
    }

    [RelayCommand]
    private void OpenFolder(DistroItemViewModel item)
    {
        if (item.Distribution.VhdPath is { } path)
        {
            AppPaths.OpenInExplorer(path);
        }
    }

    [RelayCommand]
    private async Task UnregisterAsync(DistroItemViewModel item)
    {
        var form = new FormDialog($"Delete {item.Name}?", "Delete permanently")
            .Text($"This unregisters {item.Name} and permanently deletes its disk, including every file inside it. This cannot be undone. Export it first if you might need it.")
            .Field("confirm", $"Type {item.Name} to confirm")
            .Validate(f => f["confirm"] == item.Name ? null : $"Type {item.Name} exactly to confirm.");
        form.Dialog.PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Danger;

        if (await form.ShowAsync(ui))
        {
            controller.SetKeepAlive(item.Name, false);
            await Work(item, "Deleting…", $"Could not delete {item.Name}", () => wsl.UnregisterAsync(item.Name), success: $"{item.Name} was deleted.");
        }
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        IReadOnlyList<OnlineDistribution> available = [];
        await BusyAsync("Loading available distributions…", () => ui.RunAsync("Could not load the distribution list", async () =>
            available = await wsl.GetOnlineDistributionsAsync()));
        if (available.Count == 0)
        {
            return;
        }

        var list = new System.Windows.Controls.ListBox
        {
            ItemsSource = available,
            DisplayMemberPath = nameof(OnlineDistribution.FriendlyName),
            Height = 320,
            SelectedIndex = 0,
        };
        var dialog = new Wpf.Ui.Controls.ContentDialog
        {
            Title = "Install a distribution",
            Content = new System.Windows.Controls.StackPanel
            {
                MinWidth = 420,
                Children =
                {
                    new System.Windows.Controls.TextBlock
                    {
                        Text = "Installation opens in a console window, where you'll create your Linux user.",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 12),
                    },
                    list,
                },
            },
            PrimaryButtonText = "Install",
            CloseButtonText = "Cancel",
            DefaultButton = Wpf.Ui.Controls.ContentDialogButton.Primary,
        };

        if (await ui.ShowDialogAsync(dialog) == Wpf.Ui.Controls.ContentDialogResult.Primary && list.SelectedItem is OnlineDistribution chosen)
        {
            await ui.RunAsync($"Could not install {chosen.FriendlyName}", () =>
            {
                wsl.InstallInteractive(chosen.Name);
                return Task.CompletedTask;
            });
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var form = new FormDialog("Import a distribution", "Import")
            .FileField("file", "Backup file", "WSL backups (*.tar;*.tar.gz;*.tar.xz;*.vhdx)|*.tar;*.tar.gz;*.tgz;*.tar.xz;*.vhdx|All files (*.*)|*.*")
            .Field("name", "Name")
            .FolderField("location", "Install location")
            .Validate(f => File.Exists(f["file"]) ? null : "Choose a backup file.")
            .Validate(f => DistroNames.IsValid(f["name"]) ? null : "Use letters, numbers, '.', '_' or '-' for the name.")
            .Validate(f => Distributions.Any(d => d.Name.Equals(f["name"], StringComparison.OrdinalIgnoreCase)) ? "A distribution with that name exists." : null)
            .Validate(f => string.IsNullOrEmpty(f["location"]) || Path.IsPathFullyQualified(f["location"]) ? null : "Choose a folder.");

        if (!await form.ShowAsync(ui))
        {
            return;
        }

        var name = form["name"];
        var location = string.IsNullOrEmpty(form["location"]) ? DefaultLocation(name) : form["location"];
        var isVhd = form["file"].EndsWith(".vhdx", StringComparison.OrdinalIgnoreCase);
        await BusyAsync($"Importing {name}…", () => ui.RunAsync($"Could not import {name}", async () =>
        {
            await wsl.ImportAsync(name, location, form["file"], isVhd);
            ui.Notify("Imported", $"{name} is ready.", Severity.Success);
        }));
        await RefreshAsync();
    }

    private static string DefaultLocation(string name) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wsl", name);

    private async Task Work(DistroItemViewModel item, string busyText, string failure, Func<Task> action, string? success = null)
    {
        item.IsBusy = true;
        item.BusyText = busyText;
        try
        {
            if (await ui.RunAsync(failure, action) && success is not null)
            {
                ui.Notify(item.Name, success, Severity.Success);
            }
        }
        finally
        {
            item.IsBusy = false;
            item.BusyText = null;
        }

        await controller.RefreshStatusAsync();
        await RefreshAsync();
    }
}
