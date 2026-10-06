using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PinOverlay.ViewModels;

namespace PinOverlay.Views;

/// <summary>設定画面。タブの並べ替え（ドラッグ＆ドロップ）もここで扱う。</summary>
public partial class SettingsWindow : Window
{
    private const string TabDragFormat = "pin-overlay/tab";

    private readonly AppController _controller;
    private Point? _dragStart;

    internal SettingsWindow(AppController controller)
    {
        InitializeComponent();
        _controller = controller;
        OpacitySlider.Value = Math.Round(controller.Opacity * 100);
        UpdateOpacityText();
    }

    /// <summary>タブを作り直す。選択中のタブはなるべく保つ。</summary>
    internal void SetTabs(IReadOnlyList<ImageTabViewModel> imageTabs, TextTabView textTab)
    {
        var selected = (Tabs.SelectedItem as TabItem)?.Tag;
        // テキストタブの中身は使い回すので、古いタブから外しておく
        foreach (var old in Tabs.Items.OfType<TabItem>())
        {
            old.Content = null;
        }
        Tabs.Items.Clear();

        foreach (var tab in imageTabs)
        {
            Tabs.Items.Add(CreateImageTab(tab));
        }

        var textItem = new TabItem { Header = "テキスト", Content = textTab, Tag = textTab };
        Tabs.Items.Add(textItem);

        Tabs.SelectedItem = Tabs.Items.OfType<TabItem>().FirstOrDefault(t => Equals(t.Tag, selected))
            ?? Tabs.Items.OfType<TabItem>().FirstOrDefault();
        EmptyHint.Visibility = imageTabs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private TabItem CreateImageTab(ImageTabViewModel tab)
    {
        var count = new TextBlock { Foreground = Brushes.White, FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
        count.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(ImageTabViewModel.CheckedCount)));
        var header = new StackPanel { Orientation = Orientation.Horizontal, DataContext = tab };
        header.Children.Add(new TextBlock { Text = tab.Name, VerticalAlignment = VerticalAlignment.Center });
        header.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xC0)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 0, 6, 1),
            Margin = new Thickness(6, 0, 0, 0),
            Child = count,
        });

        var item = new TabItem
        {
            Header = header,
            Content = new ImageTabView(tab),
            Tag = tab.Name,
            AllowDrop = true,
            ToolTip = "ドラッグで並べ替えられます",
        };
        item.PreviewMouseLeftButtonDown += (_, e) => _dragStart = e.GetPosition(this);
        item.PreviewMouseMove += (_, e) => OnTabMouseMove(item, e);
        item.DragOver += OnTabDragOver;
        item.Drop += (_, e) => OnTabDrop(item, e);
        return item;
    }

    private void OnTabMouseMove(TabItem item, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStart is not Point start || !IsOnHeader(item, e))
        {
            return;
        }
        var delta = e.GetPosition(this) - start;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }
        _dragStart = null;
        DragDrop.DoDragDrop(item, new DataObject(TabDragFormat, item.Tag), DragDropEffects.Move);
    }

    /// <summary>タブの中身ではなく、見出しの上でのドラッグだけを並べ替えとして扱う。</summary>
    private static bool IsOnHeader(TabItem item, MouseEventArgs e)
    {
        for (var current = e.OriginalSource as DependencyObject; current is not null && current is Visual; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ContentPresenter { Content: var content } && ReferenceEquals(content, item.Header))
            {
                return true;
            }
            if (ReferenceEquals(current, item.Content))
            {
                return false;
            }
        }
        return false;
    }

    private void OnTabDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(TabDragFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnTabDrop(TabItem target, DragEventArgs e)
    {
        if (e.Data.GetData(TabDragFormat) is not string name || target.Tag is not string targetName || name == targetName)
        {
            return;
        }
        var source = Tabs.Items.OfType<TabItem>().First(t => Equals(t.Tag, name));
        var targetIndex = Tabs.Items.IndexOf(target);
        Tabs.Items.Remove(source);
        Tabs.Items.Insert(targetIndex, source);
        Tabs.SelectedItem = source;

        var order = Tabs.Items.OfType<TabItem>().Select(t => t.Tag).OfType<string>().ToList();
        _controller.SetTabOrder(order);
    }

    private void OnReload(object sender, RoutedEventArgs e) => _controller.Reload();

    private void OnOpenFolder(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo(_controller.ImagesDirectory) { UseShellExecute = true });

    private void OnStartOverlay(object sender, RoutedEventArgs e) => _controller.EnterOverlayMode();

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_controller is null)
        {
            return;
        }
        _controller.Opacity = OpacitySlider.Value / 100;
        UpdateOpacityText();
    }

    private void UpdateOpacityText() => OpacityText.Text = $"{OpacitySlider.Value:0}%";

    /// <summary>×で閉じたらアプリを終了する（トレイの「終了」と同じ）。</summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!_controller.IsExiting)
        {
            e.Cancel = true;
            // 閉じている最中にはウィンドウを閉じ直せないので、終了処理は後で行う
            Dispatcher.BeginInvoke(_controller.Exit);
        }
    }
}
