using System.Windows;
using MonteCarloPi.Desktop.ViewModels;

namespace MonteCarloPi.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new MainWindow(new MainViewModel()).Show();
    }
}
