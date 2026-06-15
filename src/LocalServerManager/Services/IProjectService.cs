using LocalServerManager.Models;

namespace LocalServerManager.Services;

public interface IProjectService
{
    List<ProjectModel> Projects { get; }
    ProjectModel? ActiveProject { get; }
    event EventHandler? ActiveProjectChanged;
    
    void AddProject(string folderPath);
    void RemoveProject(string projectId);
    void SetActiveProject(string projectId);
    void LoadProjects();
    void SaveProjects();
}
