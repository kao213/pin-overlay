using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using PinOverlay.Controls;
using PinOverlay.Core;
using PinOverlay.Interop;
using PinOverlay.Services;
using PinOverlay.ViewModels;
using PinOverlay.Views;

namespace PinOverlay;

/// <summary>
/// アプリ全体の流れをまとめる。
/// 設定画面（編集モード）と透過表示の切り替え、かたまりの表示、設定の保存を担当する。
/// </summary>
internal sealed class AppController
{
    private const string TextOverlayKey = "\u0000text";

    private readonly string _settingsPath;
    private readonly AppSettings _settings;
    private readonly FlowDocument _textDocument;
    private readonly Dictionary<string, OverlayWindow> _overlays = [];
    private readonly DispatcherTimer _saveTimer;
    private List<ImageTabViewModel> _imageTabs = [];
    private SettingsWindow? _settingsWindow;
    private TextTabView? _textTab;
    private TrayIcon? _tray;
    private bool _editMode = true;

    public AppController(string baseDirectory)
    {
        ImagesDirectory = Path.Combine(baseDirectory, "images");
        _settingsPath = Path.Combine(baseDirectory, "settings.json");
        _settings = SettingsStore.Load(_settingsPath);
        _textDocument = RichTextSerializer.CreateDocument(_settings.Text.ContentXaml);

        // 変更のたびに書き込むと重いので、少し待ってからまとめて保存する
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _saveTimer.Tick += (_, _) => SaveNow();
    }

    public string ImagesDirectory { get; }

    public bool IsExiting { get; private set; }

    public double Opacity
    {
        get => _settings.Opacity;
        set
        {
            _settings.Opacity = Math.Clamp(value, 0.1, 1.0);
            foreach (var overlay in _overlays.Values)
            {
                overlay.Opacity = _settings.Opacity;
            }
            RequestSave();
        }
    }

    public void Start()
    {
        Directory.CreateDirectory(ImagesDirectory);

        _textTab = new TextTabView(_textDocument, () => OutlineColor, color =>
        {
            _settings.OutlineColor = color.ToString();
            RefreshOverlay(TextOverlayKey);
            RequestSave();
        });
        _textTab.ContentChanged += () =>
        {
            RefreshOverlay(TextOverlayKey);
            RequestSave();
        };

        _settingsWindow = new SettingsWindow(this, _settings.SettingsWindow);
        _tray = new TrayIcon(EnterEditMode, Exit);

        Reload();
        EnsureTextOverlay();
        EnterEditMode();
    }

    /// <summary>images フォルダを読み直して、タブとかたまりを作り直す。</summary>
    public void Reload()
    {
        var folders = ImageLibrary.Scan(ImagesDirectory).ToDictionary(f => f.Name);
        _settings.TabOrder = TabOrdering.Reconcile(_settings.TabOrder, folders.Keys);

        foreach (var tab in _imageTabs)
        {
            tab.Changed -= OnImageTabChanged;
        }
        _imageTabs = _settings.TabOrder
            .Select(name => new ImageTabViewModel(folders[name], _settings.GetOrAddTab(name)))
            .ToList();
        foreach (var tab in _imageTabs)
        {
            tab.Changed += OnImageTabChanged;
        }

        // なくなったフォルダのかたまりを閉じる
        foreach (var key in _overlays.Keys.Where(k => k != TextOverlayKey && !folders.ContainsKey(k)).ToList())
        {
            _overlays[key].CloseWithGear();
            _overlays.Remove(key);
        }

        // 中身を作る関数が新しいタブを指すよう、かたまりは作り直す（位置は引き継ぐ）
        for (var i = 0; i < _imageTabs.Count; i++)
        {
            var tab = _imageTabs[i];
            var position = tab.Settings.Position;
            if (_overlays.Remove(tab.Name, out var old))
            {
                position = old.Position;
                old.CloseWithGear();
            }
            var overlay = CreateOverlay(tab.Name, tab.Name, position, i + 1, ppd => BuildImageGroup(tab, ppd));
            _overlays[tab.Name] = overlay;
            overlay.Refresh();
        }

        _settingsWindow?.SetTabs(_imageTabs, _textTab!);
        UpdateStartButton();
        RequestSave();
    }

    public void SetTabOrder(IReadOnlyList<string> order)
    {
        _settings.TabOrder = order.ToList();
        var byName = _imageTabs.ToDictionary(t => t.Name);
        _imageTabs = order.Where(byName.ContainsKey).Select(n => byName[n]).ToList();
        RequestSave();
    }

    /// <summary>設定画面を開き、かたまりをドラッグで動かせるようにする。</summary>
    public void EnterEditMode()
    {
        // 終了処理中に、もう一度起動された合図が遅れて届いたとき、閉じた設定画面を開き直さないため
        if (IsExiting)
        {
            return;
        }
        _editMode = true;
        foreach (var overlay in _overlays.Values)
        {
            overlay.EditMode = true;
        }
        if (_settingsWindow is { } window)
        {
            window.Show();
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Activate();
        }
    }

    /// <summary>設定画面を隠し、かたまりのクリックを下のウィンドウに通す。</summary>
    public void EnterOverlayMode()
    {
        _editMode = false;
        RememberSettingsWindow();
        _settingsWindow?.Hide();
        foreach (var overlay in _overlays.Values)
        {
            overlay.EditMode = false;
        }
        SaveNow();
    }

    public void Exit()
    {
        if (IsExiting)
        {
            return;
        }
        IsExiting = true;
        RememberSettingsWindow();
        SaveNow();
        foreach (var overlay in _overlays.Values)
        {
            overlay.CloseWithGear();
        }
        _settingsWindow?.Close();
        _tray?.Dispose();
        Application.Current.Shutdown();
    }

    /// <summary>設定画面の位置と大きさを覚える（最大化中・最小化中は元に戻したときの位置と大きさ）。</summary>
    private void RememberSettingsWindow()
    {
        if (_settingsWindow is not { IsVisible: true } window)
        {
            return;
        }
        var bounds = window.RestoreBounds;
        _settings.SettingsWindow = new WindowBounds
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            Maximized = window.WindowState == WindowState.Maximized,
        };
    }

    private Color OutlineColor =>
        ColorConverter.ConvertFromString(_settings.OutlineColor) is Color color ? color : Colors.Black;

    private void EnsureTextOverlay()
    {
        _overlays[TextOverlayKey] = CreateOverlay(TextOverlayKey, "テキスト", _settings.Text.Position, 0, BuildText);
        _overlays[TextOverlayKey].Refresh();
        UpdateStartButton();
    }

    private OverlayWindow CreateOverlay(string key, string label, PixelPoint? position, int index, Func<double, FrameworkElement?> factory)
    {
        var overlay = new OverlayWindow(label, factory, position ?? WindowHelper.DefaultPosition(index), EnterEditMode)
        {
            Opacity = _settings.Opacity,
            EditMode = _editMode,
        };
        overlay.Moved += moved => OnOverlayMoved(key, moved);
        return overlay;
    }

    private void OnOverlayMoved(string key, OverlayWindow overlay)
    {
        if (key == TextOverlayKey)
        {
            _settings.Text.Position = overlay.Position;
        }
        else
        {
            _settings.GetOrAddTab(key).Position = overlay.Position;
        }
        RequestSave();
    }

    private void OnImageTabChanged(ImageTabViewModel tab)
    {
        RefreshOverlay(tab.Name);
        RequestSave();
    }

    private void RefreshOverlay(string key)
    {
        if (_overlays.TryGetValue(key, out var overlay))
        {
            overlay.Refresh();
        }
        UpdateStartButton();
    }

    private void UpdateStartButton() =>
        _settingsWindow?.SetCanStartOverlay(_overlays.Values.Any(o => o.IsShowingAnything));

    /// <summary>画像タブのかたまり。チェックした画像を横に並べ、折り返し幅を超えたら次の行へ送る（docs/design.md 5 章）。</summary>
    private static FrameworkElement? BuildImageGroup(ImageTabViewModel tab, double pixelsPerDip)
    {
        var panel = new WrapPanel();
        foreach (var item in tab.CheckedItems)
        {
            if (item.Image is not { } image)
            {
                continue;
            }
            var (width, height) = ImageSizing.Compute(image.PixelWidth, image.PixelHeight, tab.Settings.ScalePercent, tab.Settings.MaxSize);

            var picture = new Image
            {
                Source = image,
                Stretch = Stretch.Fill,
                // 実ピクセルどおりの大きさにするため、表示スケールで割って WPF の単位に直す
                Width = width / pixelsPerDip,
                Height = height / pixelsPerDip,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Top,
            };
            RenderOptions.SetBitmapScalingMode(picture, BitmapScalingMode.HighQuality);
            panel.Children.Add(picture);
        }
        if (tab.Settings.WrapWidth > 0 && panel.Children.Count > 0)
        {
            // MaxWidth でなく Width にして、編集モードの枠を折り返し幅どおりの大きさにする（画面で幅を確かめられるように）。
            // 折り返し幅より広い画像があるときは、切れないようにその画像の幅まで広げる
            var widest = panel.Children.OfType<Image>().Max(p => p.Width + p.Margin.Left + p.Margin.Right);
            panel.Width = Math.Max(tab.Settings.WrapWidth / pixelsPerDip, widest);
        }
        return panel.Children.Count == 0 ? null : panel;
    }

    private FrameworkElement? BuildText(double pixelsPerDip)
    {
        var model = RichTextModel.From(_textDocument);
        return model.IsBlank ? null : new OutlinedRichText(model, new SolidColorBrush(OutlineColor));
    }

    private void RequestSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow()
    {
        _saveTimer.Stop();
        _settings.Text.ContentXaml = RichTextSerializer.Serialize(_textDocument);
        try
        {
            SettingsStore.Save(_settingsPath, _settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"設定を保存できませんでした。\n{_settingsPath}\n\n{ex.Message}\n\n" +
                "exe を Program Files などの書き込みできない場所に置いている場合は、デスクトップやドキュメントなどに移してください。",
                "pin-overlay", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
