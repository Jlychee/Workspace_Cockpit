# Workspace Cockpit

Workspace Cockpit - небольшое Windows-приложение для управления рабочими пространствами, заметками, запускаемыми действиями и логами запусков.

Проект написан на WPF и .NET 10. Для хранения данных используются Entity Framework Core и SQLite.

## Возможности

- Создание рабочих пространств с корневой папкой и resume-текстом.
- Заметки для каждого workspace.
- Actions для команд, файлов, папок и URL.
- Запуск одной action или запуск всех actions в сохраненном порядке.
- Перетаскивание actions и notes с сохранением порядка.
- История запусков actions с превью вывода.
- Локальное хранение данных в SQLite.

## Требования

- Windows
- .NET 10 SDK

## Запуск

Восстановить зависимости:

```powershell
dotnet restore "Workspace Cockpit.sln"
```

Собрать solution:

```powershell
dotnet build "Workspace Cockpit.sln"
```

Запустить приложение:

```powershell
dotnet run --project "Workspace Cockpit\Workspace Cockpit.csproj"
```

## База данных

Workspace Cockpit использует локальную SQLite-базу. Она создается автоматически при запуске приложения, миграции также применяются автоматически.

Путь по умолчанию:

```text
%LOCALAPPDATA%\Workspace Cockpit\workspace-cockpit.db
```

## Структура проекта

```text
Workspace Cockpit/   WPF-приложение и окна
Infrastructure/      EF Core DbContext, миграции, репозитории, сервисы
Models/              Доменные сущности и enum-ы
```

## Разработка

После изменения моделей, которые сохраняются в БД, нужно добавить EF Core миграцию:

```powershell
dotnet ef migrations add MigrationName --project Infrastructure --startup-project "Workspace Cockpit"
```

Приложение применяет pending-миграции при старте через `DatabaseInitializer`.
