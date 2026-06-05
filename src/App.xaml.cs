using System.Windows;
using System.Windows.Threading;

namespace NativeMDView
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var splash = new SplashWindow();
            splash.Show();

            // Даём сплешу полностью отрисоваться
            splash.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

            // Создаём главное окно на UI-потоке
            var mainWindow = new MainWindow(e.Args);
            MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();

            splash.Close();
        }
    }
}
