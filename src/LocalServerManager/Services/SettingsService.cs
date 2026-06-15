using System.IO;
using System.Text.Json;

namespace LocalServerManager.Services;

public interface ISettingsService
{
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    
    // Paths
    string PhpPath { get; set; }
    string PythonPath { get; set; }
    
    // Server
    int DefaultPort { get; set; }
    
    // Appearance
    string Theme { get; set; }
    
    // Docker
    string DockerUri { get; set; }
    int DockerTimeout { get; set; }
    
    // Behaviour
    bool AutoStartLastProject { get; set; }
    bool MinimizeToTrayOnClose { get; set; }
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private SettingsModel _settings = new();

    public SettingsService()
    {
        LoadSettingsAsync().Wait();
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                _settings = JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            _settings = new SettingsModel();
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
        }
    }

    public string PhpPath
    {
        get => _settings.PhpPath;
        set { _settings.PhpPath = value; _ = SaveSettingsAsync(); }
    }

    public string PythonPath
    {
        get => _settings.PythonPath;
        set { _settings.PythonPath = value; _ = SaveSettingsAsync(); }
    }

    public int DefaultPort
    {
        get => _settings.DefaultPort;
        set { _settings.DefaultPort = value; _ = SaveSettingsAsync(); }
    }

    public string Theme
    {
        get => _settings.Theme;
        set { _settings.Theme = value; _ = SaveSettingsAsync(); }
    }

    public string DockerUri
    {
        get => _settings.DockerUri;
        set { _settings.DockerUri = value; _ = SaveSettingsAsync(); }
    }

    public int DockerTimeout
    {
        get => _settings.DockerTimeout;
        set { _settings.DockerTimeout = value; _ = SaveSettingsAsync(); }
    }

    public bool AutoStartLastProject
    {
        get => _settings.AutoStartLastProject;
        set { _settings.AutoStartLastProject = value; _ = SaveSettingsAsync(); }
    }

    public bool MinimizeToTrayOnClose
    {
        get => _settings.MinimizeToTrayOnClose;
        set { _settings.MinimizeToTrayOnClose = value; _ = SaveSettingsAsync(); }
    }
}

public class SettingsModel
{
    public string PhpPath { get; set; } = string.Empty;
    public string PythonPath { get; set; } = string.Empty;
    public int DefaultPort { get; set; } = 8000;
    public string Theme { get; set; } = "Dark";
    public string DockerUri { get; set; } = "npipe://./pipe/docker_engine";
    public int DockerTimeout { get; set; } = 30;
    public bool AutoStartLastProject { get; set; } = false;
    public bool MinimizeToTrayOnClose { get; set; } = true;
}
