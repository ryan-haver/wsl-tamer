using System.Collections.ObjectModel;
using System.Windows;
using WslTamer.App.Services;
using WslTamer.App.ViewModels;
using WslTamer.Core.Config;
using WslTamer.Core.Wsl;

namespace WslTamer.App.Views.Dialogs;

/// <summary>Edits a distribution's /etc/wsl.conf without losing anything the editor doesn't model.</summary>
public partial class DistroConfigWindow
{
    private readonly string _distro;
    private readonly IWslClient _wsl;
    private readonly UserInteraction _ui;
    private readonly ObservableCollection<SettingRowViewModel> _rows = [];
    private IniDocument? _document;

    public DistroConfigWindow(string distro, IWslClient wsl, UserInteraction ui)
    {
        _distro = distro;
        _wsl = wsl;
        _ui = ui;
        InitializeComponent();
        Heading.Text = distro;
        TitleBar.Title = $"{distro} settings";
        Editor.ItemsSource = _rows;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var text = await _wsl.ReadFileAsync(_distro, WslConfCatalog.FilePath);
            _document = IniDocument.Parse(text ?? string.Empty);
            foreach (var definition in WslConfCatalog.All)
            {
                var raw = _document.Get(definition.Section, definition.Key);
                _rows.Add(new SettingRowViewModel(definition, raw is null ? null : definition.FromFileValue(raw)));
            }

            var unknown = _document.Entries
                .Where(e => WslConfCatalog.Find(e.Section, e.Key) is null)
                .Select(e => $"[{e.Section}] {e.Key} = {e.Value}")
                .ToList();
            if (unknown.Count > 0)
            {
                OtherLines.Text = string.Join(Environment.NewLine, unknown);
                OtherPanel.Visibility = Visibility.Visible;
            }

            SaveButton.IsEnabled = true;
        }
        catch (Exception ex) when (ex is WslException or Core.Processes.ProcessTimeoutException or IOException)
        {
            // Never offer to save after a failed read: that would replace the real file with blanks.
            LoadError.Message = ex.Message;
            LoadError.IsOpen = true;
        }
        finally
        {
            Loading.Visibility = Visibility.Collapsed;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        if (_rows.FirstOrDefault(r => r.Error is not null) is { } invalid)
        {
            await _ui.AlertAsync("Check your settings", $"{invalid.Label}: {invalid.Error}");
            return;
        }

        foreach (var row in _rows.Where(r => r.IsChanged))
        {
            var d = row.Definition;
            if (row.Value is { } value)
            {
                _document.Set(d.Section, d.Key, d.ToFileValue(value));
            }
            else
            {
                _document.Remove(d.Section, d.Key);
            }
        }

        SaveButton.IsEnabled = false;
        bool restart = RestartAfterSave.IsChecked == true;
        bool saved = await _ui.RunAsync($"Could not save {_distro}'s settings", async () =>
        {
            await _wsl.WriteFileAsync(_distro, WslConfCatalog.FilePath, _document.ToString());
            if (restart)
            {
                await _wsl.TerminateAsync(_distro);
            }
        });

        if (saved)
        {
            _ui.Notify(_distro, restart ? "Settings saved. The distribution will use them next time it starts." : "Settings saved. Restart the distribution to apply them.", Severity.Success);
            Close();
        }
        else
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
