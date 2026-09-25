using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace WslTamer.App.Services;

/// <summary>Builds small input dialogs (text fields and folder/file pickers) in code.</summary>
public sealed class FormDialog
{
    private readonly StackPanel _panel = new() { MinWidth = 420 };
    private readonly Dictionary<string, TextBox> _fields = [];
    private readonly List<Func<string?>> _validators = [];
    private readonly TextBlock _error = new()
    {
        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"],
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 8, 0, 0),
        Visibility = Visibility.Collapsed,
    };

    public FormDialog(string title, string primaryText)
    {
        Dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = primaryText,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = _panel,
        };
        Dialog.Closing += OnClosing;
    }

    public ContentDialog Dialog { get; }

    public FormDialog Text(string message)
    {
        _panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        return this;
    }

    public FormDialog Field(string key, string label, string value = "", string? placeholder = null)
    {
        AddField(key, label, value, placeholder, browse: null);
        return this;
    }

    public FormDialog FolderField(string key, string label, string value = "")
    {
        AddField(key, label, value, null, box =>
        {
            var dialog = new OpenFolderDialog { Title = label };
            if (Directory.Exists(box.Text))
            {
                dialog.InitialDirectory = box.Text;
            }

            if (dialog.ShowDialog() == true)
            {
                box.Text = dialog.FolderName;
            }
        });
        return this;
    }

    public FormDialog FileField(string key, string label, string filter, string value = "")
    {
        AddField(key, label, value, null, box =>
        {
            var dialog = new OpenFileDialog { Title = label, Filter = filter };
            if (dialog.ShowDialog() == true)
            {
                box.Text = dialog.FileName;
            }
        });
        return this;
    }

    /// <summary>Adds a check run when the primary button is pressed. Return an error message to keep the dialog open.</summary>
    public FormDialog Validate(Func<FormDialog, string?> validator)
    {
        _validators.Add(() => validator(this));
        return this;
    }

    public string this[string key] => _fields[key].Text.Trim();

    public async Task<bool> ShowAsync(UserInteraction ui)
    {
        _panel.Children.Add(_error);
        return await ui.ShowDialogAsync(Dialog) == ContentDialogResult.Primary;
    }

    private void AddField(string key, string label, string value, string? placeholder, Action<TextBox>? browse)
    {
        _panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 8, 0, 4) });
        var box = new TextBox { Text = value, PlaceholderText = placeholder ?? string.Empty };
        _fields[key] = box;

        if (browse is null)
        {
            _panel.Children.Add(box);
            return;
        }

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var button = new Wpf.Ui.Controls.Button { Content = "Browse…", Margin = new Thickness(8, 0, 0, 0) };
        button.Click += (_, _) => browse(box);
        Grid.SetColumn(button, 1);
        row.Children.Add(box);
        row.Children.Add(button);
        _panel.Children.Add(row);
    }

    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (args.Result != ContentDialogResult.Primary)
        {
            return;
        }

        foreach (var validate in _validators)
        {
            if (validate() is { } error)
            {
                _error.Text = error;
                _error.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }
        }
    }
}
