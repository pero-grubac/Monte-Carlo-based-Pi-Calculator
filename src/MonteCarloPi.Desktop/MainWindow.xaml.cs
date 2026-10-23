using System.Windows;
using MonteCarloPi.Desktop.ViewModels;

namespace MonteCarloPi.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Drawing points is a view concern: the view model only hands them over.
        viewModel.PointsDrawn += (_, batch) => PointCloud.Draw(batch.Points.AsSpan(0, batch.Count), batch.TotalBefore);
        viewModel.Cleared += (_, _) => PointCloud.Clear();
    }
}
