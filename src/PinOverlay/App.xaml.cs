using System.Threading;
using System.Windows;

namespace PinOverlay;

public partial class App : Application
{
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 二重に起動すると settings.json を取り合うので、1 つだけにする
        var mutex = new Mutex(initiallyOwned: true, "pin-overlay-single-instance", out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            MessageBox.Show("pin-overlay はすでに起動しています。タスクトレイのアイコンから設定画面を開けます。",
                "pin-overlay", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        _singleInstance = mutex;

        new AppController(AppContext.BaseDirectory).Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_singleInstance is not null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        base.OnExit(e);
    }
}
