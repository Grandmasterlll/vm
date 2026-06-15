using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalServerManager.Models;
using LocalServerManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace LocalServerManager.ViewModels;

public partial class ServerViewModel : ObservableObject
{
    private readonly IServerManager _serverManager;
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private string _logOutput = string.Empty;

    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private bool _isStarting;

    [ObservableProperty]
    private bool _isStopping;

    [ObservableProperty]
    private string _serverStatusText = "Остановлен";

    [ObservableProperty]
    private int _processId;

    public ObservableCollection<string> Logs { get; } = new();

    public ServerViewModel(IServerManager serverManager, IProjectService projectService)
    {
        _serverManager = serverManager;
        _projectService = projectService;

        _serverManager.LogReceived += OnLogReceived;
        _serverManager.StatusChanged += OnStatusChanged;
    }

    private void OnLogReceived(object? sender, string log)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Logs.Add(log);
            if (Logs.Count > 1000)
            {
                Logs.RemoveAt(0);
            }
            LogOutput = string.Join("\n", Logs);
        });
    }

    private void OnStatusChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsServerRunning = _serverManager.Status.IsRunning;
            ServerStatusText = _serverManager.Status.Status switch
            {
                ServerStatus.Running => "Запущен",
                ServerStatus.Stopped => "Остановлен",
                ServerStatus.Starting => "Запуск...",
                ServerStatus.Stopping => "Остановка...",
                _ => ServerStatusText
            };
            ProcessId = _serverManager.Status.Pid ?? 0;
        });
    }

    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private async Task StartServerAsync()
    {
        var activeProject = _projectService.ActiveProject;
        if (activeProject == null)
        {
            MessageBox.Show("Сначала выберите активный проект во вкладке \"Проекты\"", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsStarting = true;
        try
        {
            await _serverManager.StartServerAsync(activeProject);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка запуска сервера: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsStarting = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopServer))]
    private async Task StopServerAsync()
    {
        IsStopping = true;
        try
        {
            await _serverManager.StopServerAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка остановки сервера: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsStopping = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRestartServer))]
    private async Task RestartServerAsync()
    {
        var activeProject = _projectService.ActiveProject;
        if (activeProject == null) return;

        IsStopping = true;
        try
        {
            await _serverManager.RestartServerAsync(activeProject);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка перезапуска сервера: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsStopping = false;
        }
    }

    private bool CanStartServer() => !IsServerRunning && !IsStarting && !IsStopping;
    private bool CanStopServer() => IsServerRunning && !IsStopping && !IsStarting;
    private bool CanRestartServer() => IsServerRunning && !IsStopping && !IsStarting;
}
