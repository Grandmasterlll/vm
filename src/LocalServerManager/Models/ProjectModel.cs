namespace LocalServerManager.Models;

public enum ProjectType
{
    Unknown,
    Laravel,
    Django
}

public enum ServerStatus
{
    Stopped,
    Running,
    Starting,
    Stopping
}

public class ProjectModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Path { get; set; } = string.Empty;
    public ProjectType Type { get; set; } = ProjectType.Unknown;
    public int Port { get; set; }
    public string? EnvFile { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ServerStatusModel
{
    public bool IsRunning { get; set; }
    public int? Pid { get; set; }
    public string CurrentLog { get; set; } = string.Empty;
    public ServerStatus Status { get; set; } = ServerStatus.Stopped;
}

public class ContainerModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string[]? Names { get; set; }
    public DateTime Created { get; set; }
}
