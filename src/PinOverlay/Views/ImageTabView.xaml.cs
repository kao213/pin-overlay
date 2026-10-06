using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PinOverlay.ViewModels;

namespace PinOverlay.Views;

public partial class ImageTabView : UserControl
{
    public ImageTabView(ImageTabViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private ImageTabViewModel ViewModel => (ImageTabViewModel)DataContext;

    private void OnCheckAll(object sender, RoutedEventArgs e) => ViewModel.SetAll(true);

    private void OnUncheckAll(object sender, RoutedEventArgs e) => ViewModel.SetAll(false);

    /// <summary>カードのどこを押してもチェックを切り替える（チェックボックス自身のクリックは除く）。</summary>
    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindParent<CheckBox>(source) is not null)
        {
            return;
        }
        if (sender is FrameworkElement { DataContext: ImageItemViewModel item })
        {
            item.IsChecked = !item.IsChecked;
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        for (var current = child; current is not null; current = System.Windows.Media.VisualTreeHelper.GetParent(current))
        {
            if (current is T match)
            {
                return match;
            }
            if (current is not System.Windows.Media.Visual)
            {
                return null;
            }
        }
        return null;
    }
}
