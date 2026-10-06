using System.Drawing;
using Forms = System.Windows.Forms;

namespace PinOverlay.Services;

/// <summary>タスクトレイのアイコンと右クリックメニュー。</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Icon _image;

    public TrayIcon(Action openSettings, Action exit)
    {
        _image = CreateIcon();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("設定画面を開く", null, (_, _) => openSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => exit());

        _icon = new Forms.NotifyIcon
        {
            Icon = _image,
            Text = "pin-overlay",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => openSettings();
    }

    /// <summary>exe と同じアイコン。トレイの大きさ（DPI で変わる）に合う版を ico から選ぶ。</summary>
    private static Icon CreateIcon()
    {
        using var stream = typeof(TrayIcon).Assembly.GetManifestResourceStream("PinOverlay.pin-overlay.ico")!;
        return new Icon(stream, Forms.SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _image.Dispose();
    }
}
