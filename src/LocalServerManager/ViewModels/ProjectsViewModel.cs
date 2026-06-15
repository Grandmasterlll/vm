using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalServerManager.Models;
using LocalServerManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace LocalServerManager.ViewModels;

public partial class ProjectsViewModel : ObservableObject
{
    private readonly IProjectService _projectService;

    public ObservableCollection<ProjectViewModel> Projects { get; } = new();
    
    [ObservableProperty]
    private ProjectViewModel? _selectedProject;

    public ProjectsViewModel(IProjectService projectService)
    {
        _projectService = projectService;
        _projectService.ActiveProjectChanged += OnActiveProjectChanged;
        LoadProjects();
    }

    private void OnActiveProjectChanged(object? sender, EventArgs e)
    {
        LoadProjects();
    }

    [RelayCommand]
    private void AddProject()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выберите папку с проектом"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _projectService.AddProject(dialog.FolderName);
                LoadProjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления проекта: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void RemoveProject()
    {
        if (SelectedProject == null) return;

        var result = MessageBox.Show(
            $"Удалить проект \"{SelectedProject.Name}\"?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _projectService.RemoveProject(SelectedProject.Id);
            LoadProjects();
        }
    }

    [RelayCommand]
    private void SetActiveProject()
    {
        if (SelectedProject == null) return;

        _projectService.SetActiveProject(SelectedProject.Id);
    }

    private void LoadProjects()
    {
        Projects.Clear();
        
        foreach (var project in _projectService.Projects)
        {
            var vm = new ProjectViewModel(project);
            vm.IsSelected = project.Id == _projectService.ActiveProject?.Id;
            Projects.Add(vm);
        }
    }
}

public partial class ProjectViewModel : ObservableObject
{
    public string Id { get; }
    public string Path { get; }
    
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _isSelected;

    public ProjectViewModel(ProjectModel model)
    {
        Id = model.Id;
        Path = model.Path;
        Name = Path.Split('\\').LastOrDefault() ?? "Project";
        Type = model.Type switch
        {
            ProjectType.Laravel => "Laravel",
            ProjectType.Django => "Django",
            _ => "Unknown"
        };
        Port = model.Port;
    }
}
