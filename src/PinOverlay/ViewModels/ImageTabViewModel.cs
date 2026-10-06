using System.Globalization;
using PinOverlay.Core;

namespace PinOverlay.ViewModels;

/// <summary>画像タブ 1 つ（images 直下のサブフォルダ 1 つ）。</summary>
public sealed class ImageTabViewModel : ObservableObject
{
    private readonly ImageTabSettings _settings;
    private string _scaleText;
    private string _maxSizeText;
    private string _wrapWidthText;

    public ImageTabViewModel(ImageFolder folder, ImageTabSettings settings)
    {
        _settings = settings;
        Name = folder.Name;
        var checkedFiles = new HashSet<string>(settings.CheckedFiles, StringComparer.OrdinalIgnoreCase);
        Items = folder.Files
            .Select(f => new ImageItemViewModel(this, folder.FullPath, f, checkedFiles.Contains(f)))
            .ToList();
        _scaleText = settings.ScalePercent.ToString(CultureInfo.InvariantCulture);
        _maxSizeText = settings.MaxSize.ToString(CultureInfo.InvariantCulture);
        _wrapWidthText = settings.WrapWidth.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>チェックや大きさが変わったとき（オーバーレイの描き直しと保存が必要なとき）。</summary>
    public event Action<ImageTabViewModel>? Changed;

    public string Name { get; }

    public IReadOnlyList<ImageItemViewModel> Items { get; }

    public IEnumerable<ImageItemViewModel> CheckedItems => Items.Where(i => i.IsChecked);

    public int CheckedCount => Items.Count(i => i.IsChecked);

    public ImageTabSettings Settings => _settings;

    public IReadOnlyList<int> MaxSizePresets => ImageSizing.MaxSizePresets;

    /// <summary>拡大率（%）の入力欄。正しい数値のときだけ反映する。</summary>
    public string ScaleText
    {
        get => _scaleText;
        set
        {
            if (!SetField(ref _scaleText, value))
            {
                return;
            }
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)
                && scale >= ImageSizing.MinScalePercent && scale <= ImageSizing.MaxScalePercent)
            {
                _settings.ScalePercent = scale;
                Changed?.Invoke(this);
            }
        }
    }

    /// <summary>最大サイズ（px）の入力欄。選択肢から選ぶことも、自由に入力することもできる。</summary>
    public string MaxSizeText
    {
        get => _maxSizeText;
        set
        {
            if (!SetField(ref _maxSizeText, value))
            {
                return;
            }
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)
                && size >= ImageSizing.MinMaxSize && size <= ImageSizing.MaxMaxSize)
            {
                _settings.MaxSize = size;
                Changed?.Invoke(this);
            }
        }
    }

    /// <summary>折り返し幅（px）の入力欄。0 は折り返さない。</summary>
    public string WrapWidthText
    {
        get => _wrapWidthText;
        set
        {
            if (!SetField(ref _wrapWidthText, value))
            {
                return;
            }
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
                && width >= 0 && width <= ImageSizing.MaxWrapWidth)
            {
                _settings.WrapWidth = width;
                Changed?.Invoke(this);
            }
        }
    }

    public void SetAll(bool isChecked)
    {
        foreach (var item in Items)
        {
            item.IsChecked = isChecked;
        }
    }

    internal void OnItemCheckedChanged(ImageItemViewModel item)
    {
        if (item.IsChecked)
        {
            if (!_settings.CheckedFiles.Contains(item.FileName, StringComparer.OrdinalIgnoreCase))
            {
                _settings.CheckedFiles.Add(item.FileName);
            }
        }
        else
        {
            _settings.CheckedFiles.RemoveAll(f => string.Equals(f, item.FileName, StringComparison.OrdinalIgnoreCase));
        }
        OnPropertyChanged(nameof(CheckedCount));
        Changed?.Invoke(this);
    }
}
