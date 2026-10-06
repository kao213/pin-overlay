using System.IO;
using System.Windows.Media.Imaging;
using PinOverlay.Core;
using PinOverlay.Services;

namespace PinOverlay.ViewModels;

/// <summary>画像タブの中の画像 1 枚。</summary>
public sealed class ImageItemViewModel : ObservableObject
{
    private const int ThumbnailWidth = 160;

    private readonly ImageTabViewModel _owner;
    private bool _isChecked;
    private BitmapSource? _thumbnail;
    private bool _thumbnailLoaded;
    private BitmapSource? _image;
    private bool _imageLoaded;

    public ImageItemViewModel(ImageTabViewModel owner, string folderPath, string fileName, bool isChecked)
    {
        _owner = owner;
        FileName = fileName;
        FullPath = Path.Combine(folderPath, fileName);
        _isChecked = isChecked;
    }

    public string FileName { get; }

    public string FullPath { get; }

    public string DisplayName => ImageLibrary.DisplayName(FileName);

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetField(ref _isChecked, value))
            {
                _owner.OnItemCheckedChanged(this);
            }
        }
    }

    /// <summary>設定画面に並べる縮小画像。</summary>
    public BitmapSource? Thumbnail
    {
        get
        {
            if (!_thumbnailLoaded)
            {
                _thumbnailLoaded = true;
                _thumbnail = ImageLoader.Load(FullPath, ThumbnailWidth);
            }
            return _thumbnail;
        }
    }

    public bool IsBroken => Thumbnail is null;

    /// <summary>オーバーレイに表示する元の大きさの画像。</summary>
    public BitmapSource? Image
    {
        get
        {
            if (!_imageLoaded)
            {
                _imageLoaded = true;
                _image = ImageLoader.Load(FullPath);
            }
            return _image;
        }
    }
}
