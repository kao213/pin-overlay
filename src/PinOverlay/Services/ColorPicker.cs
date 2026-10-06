using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace PinOverlay.Services;

/// <summary>Windows 標準の色の選択ダイアログ。</summary>
internal static class ColorPicker
{
    public static Color? Pick(Color initial)
    {
        using var dialog = new Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(initial.A, initial.R, initial.G, initial.B),
            FullOpen = true,
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return null;
        }
        var c = dialog.Color;
        return Color.FromArgb(255, c.R, c.G, c.B);
    }
}
