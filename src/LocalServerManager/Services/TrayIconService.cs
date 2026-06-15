using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace LocalServerManager.Services;

public interface ITrayIconService
{
    void Initialize();
    void ShowNotification(string title, string message);
    void ShowWindow();
    void HideWindow();
    void UpdateServerStatusIcon(bool isRunning);
    void Dispose();
}

public class TrayIconService : ITrayIconService, IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly IProjectService _projectService;
    private readonly IServerManager _serverManager;

    public TrayIconService(IProjectService projectService, IServerManager serverManager)
    {
        _projectService = projectService;
        _serverManager = serverManager;
    }

    public void Initialize()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = new Icon(SystemIcons.Information, 32, 32),
            ToolTipText = "Local Server Manager"
        };

        // Контекстное меню
        var contextMenu = new ContextMenu();
        
        var showItem = new MenuItem { Header = "Показать окно" };
        showItem.Click += (s, e) => ShowWindow();
        contextMenu.Items.Add(showItem);
        
        contextMenu.Items.Add(new Separator());

        var startItem = new MenuItem { Header = "Запустить сервер" };
        startItem.Click += async (s, e) =>
        {
            var activeProject = _projectService.ActiveProject;
            if (activeProject != null)
            {
                try
                {
                    await _serverManager.StartServerAsync(activeProject);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка запуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        };
        contextMenu.Items.Add(startItem);

        var stopItem = new MenuItem { Header = "Остановить сервер" };
        stopItem.Click += async (s, e) =>
        {
            try
            {
                await _serverManager.StopServerAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка остановки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        contextMenu.Items.Add(stopItem);

        contextMenu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Выход" };
        exitItem.Click += (s, e) => Application.Current.Shutdown();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;

        // Двойной клик - показать окно
        _trayIcon.TrayLeftMouseDown += (s, e) => ShowWindow();

        // Обновление иконки при изменении статуса сервера
        _serverManager.StatusChanged += OnStatusChanged;
        UpdateServerStatusIcon(_serverManager.Status.IsRunning);
    }

    private void OnStatusChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateServerStatusIcon(_serverManager.Status.IsRunning);
        });
    }

    public void UpdateServerStatusIcon(bool isRunning)
    {
        if (_trayIcon == null) return;
        _trayIcon.Icon = isRunning ? new Icon(SystemIcons.Information, 32, 32) : new Icon(SystemIcons.Warning, 32, 32);
    }

    public void ShowNotification(string title, string message)
    {
        _trayIcon?.ShowBalloonTip(title, message, BalloonIcon.Info);
    }

    public void ShowWindow()
    {
        var window = Application.Current.MainWindow;
        if (window != null)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        }
    }

    public void HideWindow()
    {
        var window = Application.Current.MainWindow;
        if (window != null)
        {
            window.Hide();
        }
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
    }
}
