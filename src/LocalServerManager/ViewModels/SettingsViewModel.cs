using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalServerManager.Services;

namespace LocalServerManager.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    [ObservableProperty]
    private string _phpPath = string.Empty;

    [ObservableProperty]
    private string _pythonPath = string.Empty;

    [ObservableProperty]
    private int _defaultPort = 8000;

    [ObservableProperty]
    private string _theme = "Dark";

    [ObservableProperty]
    private string _dockerUri = "npipe://./pipe/docker_engine";

    [ObservableProperty]
    private int _dockerTimeout = 30;

    [ObservableProperty]
    private bool _autoStartLastProject;

    [ObservableProperty]
    private bool _minimizeToTrayOnClose;

    private void LoadSettings()
    {
        PhpPath = _settingsService.PhpPath;
        PythonPath = _settingsService.PythonPath;
        DefaultPort = _settingsService.DefaultPort;
        Theme = _settingsService.Theme;
        DockerUri = _settingsService.DockerUri;
        DockerTimeout = _settingsService.DockerTimeout;
        AutoStartLastProject = _settingsService.AutoStartLastProject;
        MinimizeToTrayOnClose = _settingsService.MinimizeToTrayOnClose;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            _settingsService.PhpPath = PhpPath;
            _settingsService.PythonPath = PythonPath;
            _settingsService.DefaultPort = DefaultPort;
            _settingsService.Theme = Theme;
            _settingsService.DockerUri = DockerUri;
            _settingsService.DockerTimeout = DockerTimeout;
            _settingsService.AutoStartLastProject = AutoStartLastProject;
            _settingsService.MinimizeToTrayOnClose = MinimizeToTrayOnClose;

            System.Windows.MessageBox.Show("Настройки сохранены", "Успех", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void BrowsePhpPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выберите путь к PHP исполняемому файлу",
            Filter = "Executable Files (*.exe)|*.exe|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            PhpPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowsePythonPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выберите путь к Python исполняемому файлу",
            Filter = "Executable Files (*.exe)|*.exe|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            PythonPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void ResetSettings()
    {
        var result = System.Windows.MessageBox.Show(
            "Вы уверены, что хотите сбросить все настройки к значениям по умолчанию?",
            "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            PhpPath = string.Empty;
            PythonPath = string.Empty;
            DefaultPort = 8000;
            Theme = "Dark";
            DockerUri = "npipe://./pipe/docker_engine";
            DockerTimeout = 30;
            AutoStartLastProject = false;
            MinimizeToTrayOnClose = true;

            SaveSettingsCommand.Execute(null);
        }
    }
}

