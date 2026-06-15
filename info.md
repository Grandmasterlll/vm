План разработки на .NET WPF (Windows Presentation Foundation)
Учитывая ваше уточнение, переориентируем план на .NET WPF. Это позволит создать нативное Windows-приложение (.exe) с богатым интерфейсом, прямым доступом к системным ресурсам, процессам и Docker API. Интерфейс будет современным с использованием Material Design, а управление Docker – через официальную библиотеку Docker.DotNet.

1. Технологический стек
Компонент	Выбор	Причина
Платформа	.NET 8 (или .NET 6 LTS)	Долгосрочная поддержка, высокая производительность, нативная интеграция
UI фреймворк	WPF	Гибкая разметка XAML, поддержка современных стилей и анимаций
Архитектурный паттерн	MVVM (Model-View-ViewModel) + Community Toolkit MVVM	Разделение логики и интерфейса, удобное тестирование
Внешний вид	MaterialDesignThemes (Material Design для WPF) + Dragablz (вкладки)	Современный интерфейс с тёмной/светлой теммой, готовые контролы
Управление Docker	Docker.DotNet (официальный клиент)	Полный контроль над контейнерами, образами, сетями через REST API
Управление процессами	System.Diagnostics.Process + async/await	Запуск и остановка серверов Laravel/Django
Системный трей	Hardcodet.NotifyIcon.Wpf	Готовая иконка в трее с меню, поддержка всплывающих уведомлений
Хранение данных	JSON (System.Text.Json) + конфиг в %AppData%	Простое и кроссплатформенное (в рамках Windows) хранение проектов и настроек
Логирование	Serilog (с выводом в файл и в UI)	Гибкое логирование, легко подключить к окну логов
Сборка .exe	Встроенный компилятор .NET + MSBuild (можно использовать Squirrel для обновлений)	Создание одного .exe или установщика (ClickOnce / MSI)
Почему WPF лучше для этой задачи, чем Electron:

Нативное исполнение, низкое потребление памяти.

Прямой доступ к Windows API (трей, процессы, хэндлы окон).

Проще интегрировать с Docker (через HTTP-клиент, без прослойки Node.js).

Безопасность: легче подписывать сборки, не требуется упаковывать Chromium.

2. Архитектура приложения (MVVM)
text
App.xaml.cs (точка входа)
├─ Bootstrapper (настройка DI контейнера, инициализация окон)
├─ MainWindow (главное окно с вкладками)
├─ TrayIcon (NotifyIcon, контекстное меню)
│
Модели (Models)
├─ ProjectModel (Id, Path, Type, Port, Env, IsActive)
├─ ServerStatusModel (IsRunning, Pid, CurrentLog)
├─ ContainerModel (Id, Name, State, Image)
│
ViewModels
├─ MainViewModel (активный проект, статус, команды)
├─ ProjectsViewModel (список проектов, добавление, удаление, выбор)
├─ ServerViewModel (запуск/остановка сервера, отображение логов)
├─ DockerViewModel (список контейнеров, управление, логи контейнера)
├─ SettingsViewModel (настройки приложения)
│
Services (логика)
├─ IProjectService – загрузка/сохранение проектов, определение типа (Laravel/Django)
├─ IServerManager – запуск/остановка процессов, перехват вывода, PID tracking
├─ IDockerService – обёртка над Docker.DotNet (список, старт, стоп, логи)
├─ ISettingsService – чтение/запись настроек (JSON)
├─ ILogService – глобальное логирование (в файл + в событие для UI)
│
Views (XAML)
├─ MainWindow.xaml – с TabControl (вкладки)
├─ ProjectsView.xaml
├─ ServerView.xaml
├─ DockerView.xaml
├─ SettingsView.xaml
Взаимодействие:
View → Binding → ViewModel → Command → Service → Обновление свойств (INotifyPropertyChanged).
Сервисы запускают асинхронные операции, изменения статуса и логи передаются через события или IObservable, обновляя UI.

3. Детальная реализация ключевых модулей
3.1. Управление проектами (ProjectService)
Метод AddProject(string folderPath)

Проверяет наличие artisan → Type = Laravel

Или manage.py → Type = Django

Генерирует Id, порт по умолчанию (8000, 8001…), сохраняет в список.

Хранение – файл projects.json в %AppData%\Local\LocalServerManager.

Сериализация – System.Text.Json.

UI – ListBox с шаблоном, кнопки "Выбрать активный", "Удалить".

3.2. Запуск сервера (ServerManager)
Проблема: нужно запустить процесс, который останется жить после закрытия приложения (если пользователь захочет), но при этом управлять им (остановить). Также нужно читать stdout/stderr в реальном времени.

Решение: использовать Process с перенаправлением вывода, асинхронное чтение.

csharp
public async Task StartServer(ProjectModel project, CancellationToken token)
{
    string workingDir = project.Path;
    string fileName, arguments;
    if (project.Type == ProjectType.Laravel)
    {
        fileName = "php";
        arguments = $"artisan serve --host=127.0.0.1 --port={project.Port}";
    }
    else // Django
    {
        fileName = "python";
        arguments = $"manage.py runserver 127.0.0.1:{project.Port}";
    }

    var processStartInfo = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        WorkingDirectory = workingDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    _currentProcess = new Process { StartInfo = processStartInfo };
    _currentProcess.Start();

    // Асинхронное чтение вывода
    _ = Task.Run(() => ReadOutput(_currentProcess.StandardOutput, "OUT"));
    _ = Task.Run(() => ReadOutput(_currentProcess.StandardError, "ERR"));

    await _currentProcess.WaitForExitAsync(token);
}
Остановка:
_currentProcess.Kill(true) (убить дерево процессов, если нужно). Для более мягкой остановки можно отправить Ctrl+C (сложнее), но для dev-серверов Laravel/Django достаточно Kill.

Статус сервера отслеживается через _currentProcess.HasExited и таймер.

3.3. Docker-контейнеры (DockerService с Docker.DotNet)
Установить NuGet: Docker.DotNet

Подключение: new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient() для Windows.

Получение списка контейнеров:
await _client.Containers.ListContainersAsync(new ContainersListParameters { All = true })
Фильтровать по лейблу или показывать все.

Управление:
StartContainerAsync, StopContainerAsync, RestartContainerAsync

Логи контейнера:
await _client.Containers.GetContainerLogsAsync(id, new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = true })
Через Stream читать по строкам.

3.4. Системный трей
Библиотека: Hardcodet.NotifyIcon.Wpf

В App.xaml объявить TaskbarIcon, привязать к ViewModel.

Два состояния иконки: зелёный/красный кружок (ресурсы .ico / .png). Меняем через IconSource.

Контекстное меню:

"Показать окно" (Show/Hide MainWindow)

"Запустить сервер" – вызывает команду из ServerViewModel

"Остановить сервер"

"Выход" (закрыть приложение, при необходимости убить сервер)

При двойном клике – показать/скрыть окно.

3.5. Вкладка настроек
Хранить в appsettings.json (в папке приложения) или user.config.
Настройки:

Путь к PHP.exe (если не в PATH)

Путь к Python.exe

Порт по умолчанию

Автозапуск последнего активного проекта при старте

Тема (Light/Dark) – переключение динамически через ResourceDictionary.

Параметры Docker (URI, таймаут)

Реализация:
SettingsService читает/сохраняет JSON. ViewModel привязана к полям, кнопка "Сохранить" пишет в файл и применяет изменения (например, меняет тему).

3.6. Современный интерфейс WPF
Библиотека MaterialDesignThemes – стили, кнопки, карточки, текстовые поля, анимации.

Dragablz – для удобных вкладок (можно перетаскивать, закрывать).

Иконки – MaterialDesignIcons или FontAwesome.

Логи – ListBox с автоматической прокруткой к последнему элементу (использовать ScrollViewer и Behavior).

Тёмная тема – переключение через смену словарей ресурсов:
Application.Current.Resources.MergedDictionaries.Add с тёмной темой MaterialDesign.

Асинхронные команды – AsyncRelayCommand из CommunityToolkit.Mvvm.

Пример разметки главного окна:

xaml
<metro:MetroWindow x:Class="LocalServerManager.MainWindow"
                   xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Grid>
        <materialDesign:ColorZone Mode="PrimaryMid" />
        <TabControl>
            <TabItem Header="Проекты" Content="{Binding ProjectsView}" />
            <TabItem Header="Сервер" Content="{Binding ServerView}" />
            <TabItem Header="Docker" Content="{Binding DockerView}" />
            <TabItem Header="Настройки" Content="{Binding SettingsView}" />
        </TabControl>
    </Grid>
</metro:MetroWindow>
4. Этапы разработки (детальный план)
Этап	Задачи	Время (дни)
1	Создание решения WPF (.NET 8). Установка NuGet: CommunityToolkit.Mvvm, MaterialDesignThemes, Hardcodet.NotifyIcon.Wpf, Docker.DotNet, Serilog. Настройка DI (Microsoft.Extensions.DependencyInjection).	0,5
2	Реализация сервисов: ProjectService (работа с JSON, определение типа проекта), SettingsService. Написание моделей.	1
3	Создание главного окна с TabControl, базовыми вкладками, привязкой к MainViewModel. Внедрение Material Design, настройка тёмной темы.	1
4	Разработка ProjectsViewModel и ProjectsView: список проектов, добавление через диалог выбора папки (System.Windows.Forms.FolderBrowserDialog), удаление, кнопка "Сделать активным".	1,5
5	Реализация ServerManagerService (запуск/остановка процессов, чтение вывода). ServerViewModel и ServerView: статус (Running/Stopped), кнопки Start/Stop, окно логов (ListBox с обновлением в реальном времени).	2
6	Интеграция Docker.DotNet: DockerService (список контейнеров, старт/стоп, получение логов). DockerViewModel и DockerView: таблица контейнеров, кнопки управления, отображение логов в отдельном окне или внутри вкладки.	2
7	Реализация системного трея: NotifyIcon, контекстное меню, привязка к командам, смена иконки в зависимости от статуса сервера.	0,5
8	Вкладка настроек: настройки пути PHP/Python, порта, темы, автозапуска. Сохранение/загрузка через SettingsService. Динамическое переключение темы.	1
9	Обработка автозапуска: при старте приложения, если включено и есть активный проект – автоматически запустить сервер.	0,5
10	Логирование через Serilog (вывод в файл и через событие в UI). Добавить глобальную обработку исключений.	0,5
11	Тестирование и отладка (особенно управление процессами и Docker под Windows).	2
12	Сборка релиза: настройка проекта для компиляции в один .exe (возможно с папкой зависимостей), создание установщика (ClickOnce или MSI с помощью WiX или InnoSetup).	1
Общее время: ~13-14 дней разработки одним разработчиком (или меньше командой).

5. Особенности и рекомендации для WPF
5.1. Управление процессами Laravel/Django на Windows
Убедитесь, что php и python доступны из PATH (или задайте в настройках приложения полные пути).

Для Laravel может потребоваться дополнительно запускать npm run dev – это можно добавить как опцию.

При убийстве процесса используйте Process.Kill(). Если сервер оставляет дочерние процессы (например, php запускает воркеры), лучше использовать Kill всего дерева:

csharp
using System.Management;
private static void KillProcessTree(int pid)
{
    var searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={pid}");
    foreach (ManagementObject mo in searcher.Get())
        KillProcessTree(Convert.ToInt32(mo["ProcessID"]));
    Process.GetProcessById(pid).Kill();
}
5.2. Работа с Docker на Windows
Требуется запущенный Docker Desktop с включенным подсистемой WSL2 или Hyper-V.

Адрес сокета: npipe://./pipe/docker_engine.

Для доступа без прав администратора пользователь должен быть в группе docker-users.

В приложении следует проверять доступность Docker: попытаться ping или получить версию.

5.3. Интерфейс и многопоточность
Все долгие операции (запуск сервера, чтение логов, Docker API) должны быть асинхронными, чтобы не блокировать UI.

Обновление коллекций (например, список контейнеров) – используйте ObservableCollection и вызывайте Dispatcher.Invoke для изменений из фоновых потоков.

Для логов сервера используйте ConcurrentQueue и таймер, который опрашивает очередь и добавляет строки в ListBox (чтобы не перегружать UI тысячами событий).

5.4. Распространение .exe
Пользователь должен установить .NET 8 Desktop Runtime (если не используется самодостаточная сборка).

Можно создать self-contained приложение:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
Это даст один большой .exe, который не требует отдельного рантайма.

Иконка для .exe и трея – добавить .ico в ресурсы.

6. Пример кода (фрагменты)
Определение типа проекта:

csharp
public ProjectType DetectType(string path)
{
    if (File.Exists(Path.Combine(path, "artisan"))) return ProjectType.Laravel;
    if (File.Exists(Path.Combine(path, "manage.py"))) return ProjectType.Django;
    return ProjectType.Unknown;
}
Запуск сервера в ViewModel:

csharp
[RelayCommand]
private async Task StartServerAsync()
{
    var active = _projectService.ActiveProject;
    if (active == null) return;
    IsServerStarting = true;
    try
    {
        await _serverManager.StartAsync(active);
        IsServerRunning = true;
    }
    catch (Exception ex)
    {
        _logService.Error(ex.Message);
    }
    finally { IsServerStarting = false; }
}
DockerService – получение контейнеров:

csharp
public async Task<IEnumerable<ContainerInfo>> GetContainersAsync()
{
    var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
    return containers.Select(c => new ContainerInfo
    {
        Id = c.ID,
        Name = c.Names.FirstOrDefault()?.TrimStart('/'),
        State = c.State,
        Image = c.Image
    });
}
7. Заключение
План на .NET WPF полностью покрывает все ваши требования:

Выбор папки с проектом Laravel/Django → автоматическое добавление.

Запуск/остановка/перезапуск сервера + отображение статуса в трее (красный/зелёный).

Управление Docker-контейнерами (список, запуск, остановка, просмотр логов).

Отдельная вкладка настроек.

Современный интерфейс (Material Design).

Итоговый .exe для Windows.

Приложение будет нативным, быстрым и надёжным. Если потребуется уточнить какую-то часть (например, код для tree-kill на WPF, асинхронное чтение логов контейнера или установщик), я готов предоставить дополнительные инструкции.