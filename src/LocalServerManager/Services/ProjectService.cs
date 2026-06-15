using System.Text.Json;
using LocalServerManager.Models;
using System.IO;

namespace LocalServerManager.Services;

public class ProjectService : IProjectService
{
    private readonly string _configPath;
    private string SettingsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LocalServerManager");

    public List<ProjectModel> Projects { get; private set; } = new();
    public ProjectModel? ActiveProject { get; private set; }
    public event EventHandler? ActiveProjectChanged;

    public ProjectService()
    {
        _configPath = Path.Combine(SettingsFolder, "projects.json");
        LoadProjects();
    }

    public void AddProject(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Папка не найдена: {folderPath}");

        var projectType = DetectType(folderPath);
        if (projectType == ProjectType.Unknown)
            throw new InvalidOperationException("Не удалось определить тип проекта (Laravel/Django)");

        // Генерация порта
        int defaultPort = projectType == ProjectType.Laravel ? 8000 : 8001;
        int port = GetAvailablePort(defaultPort);

        var project = new ProjectModel
        {
            Id = Guid.NewGuid().ToString(),
            Path = folderPath,
            Type = projectType,
            Port = port,
            IsActive = Projects.Count == 0, // Первый проект становится активным
            CreatedAt = DateTime.Now
        };

        Projects.Add(project);
        
        if (project.IsActive)
            ActiveProject = project;

        SaveProjects();
    }

    public void RemoveProject(string projectId)
    {
        var project = Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null) return;

        Projects.Remove(project);

        if (project.IsActive)
        {
            var nextProject = Projects.FirstOrDefault();
            if (nextProject != null)
            {
                nextProject.IsActive = true;
                ActiveProject = nextProject;
            }
            else
            {
                ActiveProject = null;
            }
        }

        SaveProjects();
    }

    public void SetActiveProject(string projectId)
    {
        var project = Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null) return;

        foreach (var item in Projects)
        {
            item.IsActive = item.Id == projectId;
        }

        ActiveProject = project;
        SaveProjects();
        ActiveProjectChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadProjects()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                Projects = new List<ProjectModel>();
                return;
            }

            var json = File.ReadAllText(_configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            Projects = JsonSerializer.Deserialize<List<ProjectModel>>(json, options) ?? new List<ProjectModel>();
            
            var activeProject = Projects.FirstOrDefault(p => p.IsActive);

            foreach (var project in Projects)
            {
                project.IsActive = false;
            }

            if (activeProject != null)
            {
                activeProject.IsActive = true;
                ActiveProject = activeProject;
            }
            else
            {
                ActiveProject = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки проектов: {ex.Message}");
            Projects = new List<ProjectModel>();
        }
    }

    public void SaveProjects()
    {
        try
        {
            if (!Directory.Exists(SettingsFolder))
                Directory.CreateDirectory(SettingsFolder);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(Projects, options);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения проектов: {ex.Message}");
        }
    }

    private ProjectType DetectType(string path)
    {
        if (File.Exists(Path.Combine(path, "artisan")))
            return ProjectType.Laravel;
        
        if (File.Exists(Path.Combine(path, "manage.py")))
            return ProjectType.Django;
        
        return ProjectType.Unknown;
    }

    private int GetAvailablePort(int defaultPort)
    {
        var usedPorts = Projects.Select(p => p.Port).ToHashSet();
        int port = defaultPort;
        while (usedPorts.Contains(port))
        {
            port++;
            if (port > 9000)
                port = defaultPort + new Random().Next(1, 100);
        }
        return port;
    }
}
