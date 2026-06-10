using System.Windows;

namespace NativeMDView
{
    public partial class App : Application
    {
        public App()
        {
            var splash = new SplashScreen("splash.png");
            splash.Show(true);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow(e.Args);
            MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
        }
    }
}
