using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using PinOverlay.Core;
using PinOverlay.Interop;

namespace PinOverlay.Views;

/// <summary>
/// 画面の最前面に表示するかたまり 1 つ。
/// 編集モードでは枠を出してドラッグで動かせ、透過表示ではクリックを下のウィンドウに通す。
/// </summary>
internal sealed class OverlayWindow : Window
{
    private static readonly Brush FrameBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD4, 0x00));
    // 完全に透明な部分はクリックを受け取れないので、編集モードではほぼ透明な背景を敷いてドラッグできるようにする
    private static readonly Brush GrabBrush = new SolidColorBrush(Color.FromArgb(0x01, 0, 0, 0));

    private readonly Func<double, FrameworkElement?> _contentFactory;
    private readonly Grid _root;
    private readonly Border _host;
    private readonly Rectangle _frame;
    private readonly Border _tag;
    private readonly GearWindow _gear;
    private PixelPoint _initialPosition;
    private bool _editMode = true;
    private bool _hasContent;

    /// <param name="label">編集モードで枠の左上に出す名前</param>
    /// <param name="contentFactory">表示する中身を作る。引数は PixelsPerDip。表示するものがなければ null を返す</param>
    /// <param name="position">初期位置（画面上の実ピクセル）</param>
    /// <param name="onGearClick">⚙ が押されたとき</param>
    public OverlayWindow(string label, Func<double, FrameworkElement?> contentFactory, PixelPoint position, Action onGearClick)
    {
        _contentFactory = contentFactory;
        _initialPosition = position;

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        UseLayoutRounding = true;
        Title = label;

        _host = new Border { Padding = new Thickness(6) };
        _frame = new Rectangle
        {
            Stroke = FrameBrush,
            StrokeThickness = 2,
            StrokeDashArray = [3, 2],
            IsHitTestVisible = false,
        };
        _tag = new Border
        {
            Background = FrameBrush,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(4, 1, 4, 1),
            IsHitTestVisible = false,
            Child = new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.Black },
        };
        _root = new Grid();
        _root.Children.Add(_host);
        _root.Children.Add(_frame);
        _root.Children.Add(_tag);
        Content = _root;

        _gear = new GearWindow(onGearClick);

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        LocationChanged += (_, _) => UpdateGear();
        SizeChanged += (_, _) => UpdateGear();
        IsVisibleChanged += (_, _) => UpdateGear();
    }

    /// <summary>ドラッグで動かし終わったとき、または画面外から戻したとき。</summary>
    public event Action<OverlayWindow>? Moved;

    /// <summary>現在の位置（画面上の実ピクセル）。</summary>
    public PixelPoint Position
    {
        get
        {
            if (new WindowInteropHelper(this).Handle == 0)
            {
                return _initialPosition;
            }
            var rect = WindowHelper.GetRect(this);
            return new PixelPoint(rect.Left, rect.Top);
        }
    }

    public bool EditMode
    {
        get => _editMode;
        set
        {
            _editMode = value;
            ApplyMode();
        }
    }

    /// <summary>中身を作り直す。表示するものがなければ隠す。</summary>
    public void Refresh()
    {
        var content = _contentFactory(VisualTreeHelper.GetDpi(this).PixelsPerDip);
        _host.Child = content;
        _hasContent = content is not null;
        if (!_hasContent)
        {
            Hide();
            return;
        }
        if (!IsVisible)
        {
            Show();
            if (WindowHelper.EnsureOnScreen(this, 0))
            {
                Moved?.Invoke(this);
            }
        }
        UpdateGear();
    }

    public void CloseWithGear()
    {
        _gear.Close();
        Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHelper.SetExStyle(this, NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE, true);
        WindowHelper.MoveTo(this, _initialPosition.X, _initialPosition.Y);
        ApplyMode();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        // モニターの表示スケールが変わっても、画像を実ピクセルどおりの大きさに保つ
        if (_hasContent)
        {
            _host.Child = _contentFactory(newDpi.PixelsPerDip);
        }
    }

    private void ApplyMode()
    {
        var visibility = _editMode ? Visibility.Visible : Visibility.Collapsed;
        _frame.Visibility = visibility;
        _tag.Visibility = visibility;
        _root.Background = _editMode ? GrabBrush : Brushes.Transparent;
        WindowHelper.SetExStyle(this, NativeMethods.WS_EX_TRANSPARENT, !_editMode);
        UpdateGear();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_editMode)
        {
            return;
        }
        DragMove();
        Moved?.Invoke(this);
    }

    private void UpdateGear()
    {
        if (_editMode || !IsVisible)
        {
            _gear.Hide();
            return;
        }
        if (!_gear.IsVisible)
        {
            // 持ち主にしておくと、⚙ が常にかたまりより手前に出る
            _gear.Owner ??= this;
            _gear.Show();
        }
        // かたまりの右上の角の内側に置く
        var rect = WindowHelper.GetRect(this);
        var gear = WindowHelper.GetRect(_gear);
        WindowHelper.MoveTo(_gear, rect.Right - gear.Width, rect.Top);
    }
}
