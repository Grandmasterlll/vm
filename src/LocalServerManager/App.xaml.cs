using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using LocalServerManager.Services;
using LocalServerManager.ViewModels;

namespace LocalServerManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Инициализация Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Приложение LocalServerManager запускается...");

            // Настройка DI-контейнера
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            // Показ главного окна через сервис
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Критическая ошибка при запуске приложения");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Сервисы
        services.AddSingleton<IProjectService, ProjectService>();
        // services.AddSingleton<IServerManager, ServerManager>();
        // services.AddSingleton<IDockerService, DockerService>();
        // services.AddSingleton<ISettingsService, SettingsService>();
        // services.AddSingleton<ILogService, LogService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Главное окно
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Приложение закрывается...");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

