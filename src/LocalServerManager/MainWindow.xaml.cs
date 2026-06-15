using LocalServerManager.ViewModels;
using System.Windows;

namespace LocalServerManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(
        MainViewModel mainViewModel,
        ProjectsViewModel projectsViewModel,
        ServerViewModel serverViewModel,
        DockerViewModel dockerViewModel,
        SettingsViewModel settingsViewModel)
    {
        InitializeComponent();

        DataContext = mainViewModel;
        ProjectsTab.DataContext = projectsViewModel;
        ServerTab.DataContext = serverViewModel;
        DockerTab.DataContext = dockerViewModel;
        SettingsTab.DataContext = settingsViewModel;
    }
}