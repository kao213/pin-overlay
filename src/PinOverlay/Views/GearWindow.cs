using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PinOverlay.Interop;

namespace PinOverlay.Views;

/// <summary>
/// 透過表示中のかたまりの右上に出す ⚙ ボタン。
/// かたまり本体はクリックを下に通すので、⚙ だけは別のウィンドウにしてクリックを受け取る。
/// </summary>
internal sealed class GearWindow : Window
{
    public GearWindow(Action onClick)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        Width = 22;
        Height = 22;
        Cursor = Cursors.Hand;
        ToolTip = "設定画面を開く";

        Content = new Border
        {
            CornerRadius = new CornerRadius(11),
            Background = new SolidColorBrush(Color.FromArgb(0xB0, 0x20, 0x20, 0x20)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = "⚙",
                FontSize = 13,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        MouseLeftButtonUp += (_, _) => onClick();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHelper.SetExStyle(this, NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE, true);
    }
}
