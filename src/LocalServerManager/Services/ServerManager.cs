using System.Diagnostics;
using System.IO;
using System.Management;
using LocalServerManager.Models;

namespace LocalServerManager.Services;

public class ServerManager : IServerManager
{
    private readonly ISettingsService _settingsService;
    private Process? _currentProcess;
    private CancellationTokenSource? _logReadCancellation;

    public ServerStatusModel Status { get; private set; } = new();
    public event EventHandler<string>? LogReceived;
    public event EventHandler? StatusChanged;

    public ServerManager(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task StartServerAsync(ProjectModel project, CancellationToken cancellationToken = default)
    {
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            LogReceived?.Invoke(this, "Сервер уже запущен");
            return;
        }

        UpdateStatus(ServerStatus.Starting);

        try
        {
            string fileName, arguments;

            if (project.Type == ProjectType.Laravel)
            {
                fileName = string.IsNullOrWhiteSpace(_settingsService.PhpPath) ? "php" : _settingsService.PhpPath;
                arguments = $"artisan serve --host=127.0.0.1 --port={project.Port}";
            }
            else if (project.Type == ProjectType.Django)
            {
                fileName = string.IsNullOrWhiteSpace(_settingsService.PythonPath) ? "python" : _settingsService.PythonPath;
                arguments = $"manage.py runserver 127.0.0.1:{project.Port}";
            }
            else
            {
                throw new InvalidOperationException("Неизвестный тип проекта");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = project.Path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            _currentProcess = new Process { StartInfo = processStartInfo };
            _currentProcess.EnableRaisingEvents = true;

            _currentProcess.Exited += (s, e) =>
            {
                UpdateStatus(ServerStatus.Stopped);
                LogReceived?.Invoke(this, "Сервер остановлен");
            };

            _logReadCancellation = new CancellationTokenSource();
            var logToken = _logReadCancellation.Token;

            _ = Task.Run(() => ReadOutputAsync(_currentProcess.StandardOutput, "OUT", logToken));
            _ = Task.Run(() => ReadOutputAsync(_currentProcess.StandardError, "ERR", logToken));

            _currentProcess.Start();
            Status.Pid = _currentProcess.Id;

            LogReceived?.Invoke(this, $"Сервер запущен (PID: {_currentProcess.Id})");
            UpdateStatus(ServerStatus.Running);

            // Ждём завершения процесса
            await _currentProcess.WaitForExitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, $"Ошибка запуска: {ex.Message}");
            UpdateStatus(ServerStatus.Stopped);
            throw;
        }
    }

    public async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        if (_currentProcess == null || _currentProcess.HasExited)
        {
            LogReceived?.Invoke(this, "Сервер не запущен");
            return;
        }

        UpdateStatus(ServerStatus.Stopping);

        try
        {
            // Отменяем чтение логов
            _logReadCancellation?.Cancel();

            // Убиваем процесс и его дерево
            KillProcessTree(_currentProcess.Id);

            await Task.Delay(1000, cancellationToken);

            if (!_currentProcess.HasExited)
            {
                _currentProcess.Kill();
            }

            LogReceived?.Invoke(this, "Сервер остановлен");
            UpdateStatus(ServerStatus.Stopped);
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, $"Ошибка остановки: {ex.Message}");
        }
        finally
        {
            _currentProcess?.Dispose();
            _currentProcess = null;
        }
    }

    public async Task RestartServerAsync(ProjectModel project, CancellationToken cancellationToken = default)
    {
        await StopServerAsync(cancellationToken);
        await Task.Delay(500, cancellationToken); // Небольшая пауза перед перезапуском
        await StartServerAsync(project, cancellationToken);
    }

    private void UpdateStatus(ServerStatus status)
    {
        Status.Status = status;
        Status.IsRunning = status == ServerStatus.Running;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ReadOutputAsync(StreamReader reader, string prefix, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(token);
                if (line == null)
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    LogReceived?.Invoke(this, $"[{prefix}] {line}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при остановке
        }
        catch (Exception ex)
        {
            LogReceived?.Invoke(this, $"Ошибка чтения логов: {ex.Message}");
        }
    }

    private static void KillProcessTree(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            if (process != null)
            {
                KillChildren(process);
                process.Kill();
                process.WaitForExit(2000);
            }
        }
        catch
        {
            // Процесс уже завершён
        }
    }

    private static void KillChildren(Process process)
    {
        try
        {
            var children = Process.GetProcesses().Where(p =>
            {
                try { return p.Parent()?.Id == process.Id; }
                catch { return false; }
            }).ToList();

            foreach (var child in children)
            {
                KillChildren(child);
                try { child.Kill(); } catch { }
            }
        }
        catch
        {
            // Игнорируем ошибки при доступе к процессам
        }
    }
}

// Расширение для получения родителя процесса
public static class ProcessExtensions
{
    public static Process? Parent(this Process process)
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                $"SELECT * FROM Win32_Process WHERE ProcessID = {process.Id}");

            foreach (var obj in searcher.Get())
            {
                using var mo = (System.Management.ManagementObject)obj;
                var parentPid = Convert.ToInt32(mo["ParentProcessId"]);
                return Process.GetProcessById(parentPid);
            }
        }
        catch { }
        return null;
    }
}
