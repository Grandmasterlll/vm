# Local Server Manager

Нативное WPF-приложение для управления локальными серверами Laravel/Django и Docker-контейнерами на Windows.

## 🚀 Особенности

- **Управление проектами Laravel и Django**
  - Автоматическое определение типа проекта по наличию `artisan` или `manage.py`
  - Добавление/удаление проектов через выбор папки
  - Автоматическая генерация портов
  - Сохранение списка проектов в `%AppData%`

- **Запуск/остановка серверов**
  - Запуск Laravel через `php artisan serve`
  - Запуск Django через `python manage.py runserver`
  - Отображение логов в реальном времени
  - Статус сервера в системном трее

- **Управление Docker-контейнерами**
  - Список всех контейнеров
  - Запуск, остановка, перезапуск контейнеров
  - Просмотр логов контейнеров
  - Интеграция через Docker.DotNet

- **Современный интерфейс**
  - Material Design с тёмной/светлой темой
  - Вкладки для разных функциональных блоков
  - Иконка в системном трее с контекстным меню
  - Анимации и плавные переходы

- **Настройки**
  - Путь к PHP.exe и Python.exe
  - Порт по умолчанию
  - Автозапуск последнего активного проекта
  - Переключение темы

## 🛠 Технологический стек

| Компонент | Технология |
|-----------|------------|
| Платформа | .NET 10 (или .NET 8 LTS) |
| UI фреймворк | WPF (Windows Presentation Foundation) |
| Архитектура | MVVM + Community Toolkit MVVM |
| Внешний вид | MaterialDesignThemes + Dragablz |
| Управление Docker | Docker.DotNet (официальный клиент) |
| Управление процессами | System.Diagnostics.Process + async/await |
| Системный трей | Hardcodet.NotifyIcon.Wpf |
| Хранение данных | JSON (System.Text.Json) |
| Логирование | Serilog (файл + UI) |

## 📋 Требования

- Windows 10/11
- .NET 10 Desktop Runtime (или .NET 8 LTS)
- Docker Desktop с включённым WSL2 или Hyper-V
- PHP 8.x (для Laravel проектов)
- Python 3.x (для Django проектов)

## 🚦 Установка и запуск

### 1. Перейти в директорию проекта

```powershell
cd vm
```

### 2. Восстановление зависимостей

```bash
dotnet restore
```

### 3. Запуск приложения

```bash
dotnet run --project src/LocalServerManager/LocalServerManager.csproj
```

### 4. Публикация релиза

```powershell
# Self-contained сборка для Windows x64
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/LocalServerManager
```

## 📁 Структура проекта

```
vm/
├── .gitignore
├── README.md
├── version
├── stage
├── info.md
└── src/
    └── LocalServerManager/
        ├── LocalServerManager.csproj
        ├── App.xaml
        ├── App.xaml.cs
        ├── MainWindow.xaml
        ├── MainWindow.xaml.cs
        ├── Models/
        │   └── ProjectModel.cs
        ├── ViewModels/
        │   ├── BaseViewModel.cs
        │   └── MainViewModel.cs
        ├── Services/
        │   ├── IProjectService.cs
        │   └── ProjectService.cs
        ├── Views/
        └── Logs/
```

## 🏗 Архитектура

```
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
├─ IProjectService – загрузка/сохранение проектов, определение типа
├─ IServerManager – запуск/остановка процессов, перехват вывода, PID tracking
├─ IDockerService – обёртка над Docker.DotNet
├─ ISettingsService – чтение/запись настроек (JSON)
├─ ILogService – глобальное логирование (в файл + в событие для UI)
```

## 📝 План разработки

См. файл [`stage`](stage) для отслеживания этапов разработки.

## 📌 Известные ограничения

- Работает только на Windows (WPF специфичен для Windows)
- Требуется запущенный Docker Desktop для управления контейнерами
- PHP и Python должны быть доступны в PATH или указаны в настройках

## 🤝 Вклад

1. Форкните репозиторий
2. Создайте ветку для вашей фичи (`git checkout -b feature/AmazingFeature`)
3. Закоммитьте изменения (`git commit -m 'Add some AmazingFeature'`)
4. Отправьте изменения в ветку (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request

## 📄 Лицензия

Проект разрабатывается как внутренний инструмент.

## 📮 Контакты

NLP-Core-Team

---

**Разработка:** 2024-2025 | NLP-Core-Team
