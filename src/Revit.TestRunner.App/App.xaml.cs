using System.Windows;

namespace Revit.TestRunner.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup( object sender, StartupEventArgs e )
        {
            MainWindow window = new MainWindow { DataContext = new MainWindowViewModel() };
            window.Show();
        }
    }
}
