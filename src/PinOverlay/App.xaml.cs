using System.Threading;
using System.Windows;
using PinOverlay.Interop;

namespace PinOverlay;

public partial class App : Application
{
    private const string ShowSettingsSignalName = "pin-overlay-show-settings";

    private Mutex? _singleInstance;
    private EventWaitHandle? _showSettingsSignal;
    private RegisteredWaitHandle? _showSettingsWait;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 二重に起動すると settings.json を取り合うので、1 つだけにする
        var mutex = new Mutex(initiallyOwned: true, "pin-overlay-single-instance", out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            ShowSettingsOfRunningInstance();
            Shutdown();
            return;
        }
        _singleInstance = mutex;

        var controller = new AppController(AppContext.BaseDirectory);
        controller.Start();
        // もう一度 exe を起動されたら設定画面を開く。⚙ が見つからず、トレイのアイコンも隠れていて透過から戻れないときの逃げ道
        _showSettingsSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsSignalName);
        _showSettingsWait = ThreadPool.RegisterWaitForSingleObject(_showSettingsSignal,
            (_, _) => Dispatcher.BeginInvoke(controller.EnterEditMode), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showSettingsWait?.Unregister(null);
        _showSettingsSignal?.Dispose();
        if (_singleInstance is not null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        base.OnExit(e);
    }

    /// <summary>起動中の pin-overlay に設定画面を開かせる。</summary>
    private static void ShowSettingsOfRunningInstance()
    {
        if (!EventWaitHandle.TryOpenExisting(ShowSettingsSignalName, out var signal))
        {
            // 起動中のほうがまだ準備できていない（同時に起動した）ときだけここに来る
            MessageBox.Show("pin-overlay はすでに起動しています。タスクトレイのアイコンから設定画面を開けます。",
                "pin-overlay", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        using (signal)
        {
            // いま前面にいるのはこちらなので、起動中のほうが設定画面を前面に出せるよう許可しておく
            NativeMethods.AllowSetForegroundWindow(NativeMethods.ASFW_ANY);
            signal.Set();
        }
    }
}
