using System.Windows;
using System.Windows.Interop;
using PinOverlay.Core;
using static PinOverlay.Interop.NativeMethods;

namespace PinOverlay.Interop;

/// <summary>Win32 API を使ったウィンドウ操作。位置はすべて画面上の実ピクセルで扱う。</summary>
internal static class WindowHelper
{
    public static nint Handle(Window window) => new WindowInteropHelper(window).Handle;

    /// <summary>拡張ウィンドウスタイルのフラグを付け外しする。</summary>
    public static void SetExStyle(Window window, long flags, bool enabled)
    {
        var hwnd = Handle(window);
        if (hwnd == 0)
        {
            return;
        }
        var style = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        var updated = enabled ? style | flags : style & ~flags;
        if (updated != style)
        {
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, (nint)updated);
        }
    }

    public static RECT GetRect(Window window)
    {
        GetWindowRect(Handle(window), out var rect);
        return rect;
    }

    public static void MoveTo(Window window, int x, int y)
    {
        SetWindowPos(Handle(window), 0, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>
    /// ウィンドウがどのモニターにも重なっていなければ、メインモニターの作業領域の中へ移す。
    /// 移した場合は true を返す。
    /// </summary>
    public static bool EnsureOnScreen(Window window, int fallbackIndex)
    {
        var rect = GetRect(window);
        if (MonitorFromRect(rect, MONITOR_DEFAULTTONULL) != 0)
        {
            return false;
        }
        var origin = DefaultPosition(fallbackIndex);
        MoveTo(window, origin.X, origin.Y);
        return true;
    }

    /// <summary>まだ位置が決まっていないかたまりの初期位置。メインモニター左上から少しずつずらす。</summary>
    public static PixelPoint DefaultPosition(int index)
    {
        var work = PrimaryWorkArea();
        var offset = 80 + (index % 8) * 60;
        return new PixelPoint(work.Left + offset, work.Top + offset);
    }

    /// <summary>指定した位置・大きさの四角が、いちばん近いモニターの作業領域からはみ出さない位置を返す。</summary>
    public static PixelPoint ClampToWorkArea(int x, int y, int width, int height)
    {
        var rect = new RECT { Left = x, Top = y, Right = x + width, Bottom = y + height };
        var work = WorkArea(MonitorFromRect(rect, MONITOR_DEFAULTTONEAREST));
        // モニター情報を取れなかったとき（作業領域が空）に Math.Clamp が例外を出さないよう、上限を下限より小さくしない
        return new PixelPoint(
            Math.Clamp(x, work.Left, Math.Max(work.Left, work.Right - width)),
            Math.Clamp(y, work.Top, Math.Max(work.Top, work.Bottom - height)));
    }

    private static RECT PrimaryWorkArea() => WorkArea(MonitorFromPoint(default, MONITOR_DEFAULTTOPRIMARY));

    private static RECT WorkArea(nint monitor)
    {
        var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref info);
        return info.rcWork;
    }
}
