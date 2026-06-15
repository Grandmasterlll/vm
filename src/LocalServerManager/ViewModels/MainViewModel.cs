using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LocalServerManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Local Server Manager";

    [ObservableProperty]
    private int _activeTabIndex = 0;

    [RelayCommand]
    private void NavigateToProjects()
    {
        ActiveTabIndex = 0;
    }

    [RelayCommand]
    private void NavigateToServer()
    {
        ActiveTabIndex = 1;
    }

    [RelayCommand]
    private void NavigateToDocker()
    {
        ActiveTabIndex = 2;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        ActiveTabIndex = 3;
    }
}
