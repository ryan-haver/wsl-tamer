using System.Collections;
using System.Windows;
using System.Windows.Data;

namespace WslTamer.App.Views.Controls;

/// <summary>A list of <see cref="ViewModels.SettingRowViewModel"/> editors grouped under headings.</summary>
public partial class SettingsEditor
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SettingsEditor), new PropertyMetadata(null, OnItemsSourceChanged));

    public SettingsEditor() => InitializeComponent();

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (SettingsEditor)d;
        if (e.NewValue is not IList list)
        {
            editor.List.ItemsSource = null;
            return;
        }

        var view = new ListCollectionView(list);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ViewModels.SettingRowViewModel.Group)));
        editor.List.ItemsSource = view;
    }
}
