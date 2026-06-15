using Docker.DotNet;
using Docker.DotNet.Models;
using LocalServerManager.Models;

namespace LocalServerManager.Services;

public interface IDockerService
{
    bool IsAvailable { get; }
    event EventHandler? DockerStatusChanged;
    
    Task<List<ContainerModel>> GetContainersAsync(bool all = true, CancellationToken cancellationToken = default);
    Task StartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task StopContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task RestartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<string> GetContainerLogsAsync(string containerId, int tail = 100, CancellationToken cancellationToken = default);
    Task ConnectToDockerAsync();
}
