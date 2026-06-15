using LocalServerManager.Models;

namespace LocalServerManager.Services;

public interface IServerManager
{
    ServerStatusModel Status { get; }
    event EventHandler<string>? LogReceived;
    event EventHandler? StatusChanged;
    
    Task StartServerAsync(ProjectModel project, CancellationToken cancellationToken = default);
    Task StopServerAsync(CancellationToken cancellationToken = default);
    Task RestartServerAsync(ProjectModel project, CancellationToken cancellationToken = default);
}
