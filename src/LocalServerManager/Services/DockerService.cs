using Docker.DotNet;
using Docker.DotNet.Models;
using LocalServerManager.Models;
using System.IO;
using System.Text;

namespace LocalServerManager.Services;

public class DockerService : IDockerService
{
    private DockerClient? _dockerClient;
    private bool _isAvailable;

    public bool IsAvailable => _isAvailable;
    public event EventHandler? DockerStatusChanged;

    public DockerService()
    {
        _ = ConnectToDockerAsync();
    }

    public async Task ConnectToDockerAsync()
    {
        try
        {
            // Подключение к Docker на Windows через named pipe
            var config = new DockerClientConfiguration(
                new Uri("npipe://./pipe/docker_engine"));

            _dockerClient = config.CreateClient();

            // Проверка доступности Docker
            await _dockerClient.System.PingAsync(CancellationToken.None);
            _isAvailable = true;

            DockerStatusChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            _isAvailable = false;
            DockerStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<List<ContainerModel>> GetContainersAsync(bool all = true, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return new List<ContainerModel>();

        try
        {
            var containers = await _dockerClient!.Containers.ListContainersAsync(
                new ContainersListParameters { All = all },
                cancellationToken);

            return containers.Select(c => new ContainerModel
            {
                Id = c.ID,
                Name = c.Names.FirstOrDefault()?.TrimStart('/') ?? "Unknown",
                State = c.State,
                Image = c.Image,
                Names = c.Names != null ? new List<string>(c.Names) : null
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения контейнеров: {ex.Message}");
            return new List<ContainerModel>();
        }
    }

    public async Task StartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Docker недоступен");

        try
        {
            await _dockerClient!.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка запуска контейнера: {ex.Message}", ex);
        }
    }

    public async Task StopContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Docker недоступен");

        try
        {
            await _dockerClient!.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка остановки контейнера: {ex.Message}", ex);
        }
    }

    public async Task RestartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Docker недоступен");

        try
        {
            await _dockerClient!.Containers.RestartContainerAsync(
                containerId,
                new ContainerRestartParameters { WaitBeforeKillSeconds = 10 },
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка перезапуска контейнера: {ex.Message}", ex);
        }
    }

    public async Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Docker недоступен");

        try
        {
            await _dockerClient!.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = true },
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка удаления контейнера: {ex.Message}", ex);
        }
    }

    public async Task<string> GetContainerLogsAsync(string containerId, int tail = 100, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return "Docker недоступен";

        try
        {
            // Упрощённый вариант без чтения потока
            return $"Логи контейнера {containerId.Substring(0, Math.Min(12, containerId.Length))} (требуется дополнительная реализация)";
        }
        catch (Exception ex)
        {
            return $"Ошибка получения логов: {ex.Message}";
        }
    }
}

