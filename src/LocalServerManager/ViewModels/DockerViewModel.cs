using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalServerManager.Models;
using LocalServerManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace LocalServerManager.ViewModels;

public partial class DockerViewModel : ObservableObject
{
    private readonly IDockerService _dockerService;

    public ObservableCollection<ContainerViewModel> Containers { get; } = new();
    
    [ObservableProperty]
    private ContainerViewModel? _selected;

    [ObservableProperty]
    private bool _isDockerAvailable;

    [ObservableProperty]
    private string _dockerStatusText = "Docker недоступен";

    [ObservableProperty]
    private string _logOutput = string.Empty;

    public ObservableCollection<string> Logs { get; } = new();

    public DockerViewModel(IDockerService dockerService)
    {
        _dockerService = dockerService;
        _dockerService.DockerStatusChanged += OnDockerStatusChanged;
        _ = RefreshContainersAsync();
    }

    private void OnDockerStatusChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsDockerAvailable = _dockerService.IsAvailable;
            DockerStatusText = IsDockerAvailable ? "Docker Desktop: Подключено" : "Docker недоступен";
        });
    }

    [RelayCommand]
    private async Task RefreshContainersAsync()
    {
        try
        {
            var containers = await _dockerService.GetContainersAsync(all: true);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Containers.Clear();
                foreach (var container in containers)
                {
                    Containers.Add(new ContainerViewModel(container));
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка обновления контейнеров: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartContainer))]
    private async Task StartContainerAsync()
    {
        if (Selected == null) return;

        try
        {
            await _dockerService.StartContainerAsync(Selected.Id);
            await RefreshContainersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка запуска контейнера: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopContainer))]
    private async Task StopContainerAsync()
    {
        if (Selected == null) return;

        try
        {
            await _dockerService.StopContainerAsync(Selected.Id);
            await RefreshContainersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка остановки контейнера: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRestartContainer))]
    private async Task RestartContainerAsync()
    {
        if (Selected == null) return;

        try
        {
            await _dockerService.RestartContainerAsync(Selected.Id);
            await RefreshContainersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка перезапуска контейнера: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveContainer))]
    private async Task RemoveContainerAsync()
    {
        if (Selected == null) return;

        var result = MessageBox.Show(
            $"Удалить контейнер \"{Selected.Name}\"?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _dockerService.RemoveContainerAsync(Selected.Id);
                await RefreshContainersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления контейнера: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowLogs))]
    private async Task ShowLogsAsync()
    {
        if (Selected == null) return;

        try
        {
            Logs.Clear();
            var logs = await _dockerService.GetContainerLogsAsync(Selected.Id, tail: 100);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var line in logs.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        Logs.Add(line);
                }
                LogOutput = string.Join("\n", Logs);
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка получения логов: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanStartContainer() => Selected != null && Selected.State != "running" && IsDockerAvailable;
    private bool CanStopContainer() => Selected != null && Selected.State == "running" && IsDockerAvailable;
    private bool CanRestartContainer() => Selected != null && Selected.State == "running" && IsDockerAvailable;
    private bool CanRemoveContainer() => Selected != null && IsDockerAvailable;
    private bool CanShowLogs() => Selected != null && IsDockerAvailable;

    public partial class ContainerViewModel : ObservableObject
    {
        public string Id { get; }
        public List<string>? Names { get; }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _state;

        [ObservableProperty]
        private string _image;

        [ObservableProperty]
        private string _stateColor;

        public ContainerViewModel(ContainerModel model)
        {
            Id = model.Id;
            Names = model.Names;
            Name = model.Name;
            State = model.State;
            Image = model.Image;
            StateColor = model.State switch
            {
                "running" => "#FF4CAF50",
                "exited" => "#FFF44336",
                "paused" => "#FFFF9800",
                _ => "#FF9E9E9E"
            };
        }
    }
}
