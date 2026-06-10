# File Database Task

Выбрано задание **2.4. Приложение база данных файлов**.

Приложение написано на C# / WinForms. Для хранения результатов используется PostgreSQL, структура базы описана в `db/schema.sql`.

## Возможности

- выбор папки и рекурсивное сканирование файловой системы;
- отображение файлов и папок: имя, тип, относительный путь, размер, родительская папка;
- подсчет количества файлов и общего размера для папок;
- сохранение результата сканирования в базу данных с путем и временем сканирования;
- загрузка ранее сохраненных сканирований;
- сравнение сохраненного результата с текущим состоянием папки;
- отображение статусов: новый, удален, изменился размер, изменилась папка.

Сканирование выполняется в фоне. В интерфейсе есть прогресс и отмена операции. Недоступные файлы и папки не прерывают сканирование, а выводятся как предупреждения. Reparse points и symlink-переходы пропускаются.

## Запуск

Поднять PostgreSQL:

```bash
make up
```

Строка подключения по умолчанию:

```text
Host=localhost;Port=54325;Database=file_scans;Username=file_scans;Password=file_scans
```

После запуска базы откройте `FileDatabaseTask.sln` в Visual Studio и запустите проект `FileDatabaseTask`.

При необходимости строку подключения можно переопределить через переменную окружения:

```text
FILE_DATABASE_CONNECTION_STRING=Host=localhost;Port=54325;Database=file_scans;Username=file_scans;Password=file_scans
```

## Сборка

WinForms-приложение рассчитано на запуск под Windows. Сборку можно проверить в Visual Studio или командой:

```bash
dotnet build FileDatabaseTask.sln /p:EnableWindowsTargeting=true --configfile NuGet.config
```

Если .NET SDK не установлен локально, можно использовать Docker:

```bash
docker run --rm -e DOTNET_NUGET_SIGNATURE_VERIFICATION=false -v "$PWD:/workspace" -w /workspace mcr.microsoft.com/dotnet/sdk:8.0 dotnet build FileDatabaseTask.sln /p:EnableWindowsTargeting=true --configfile NuGet.config
```

## Зависимости

Используется один NuGet-пакет: `Npgsql`, ADO.NET-провайдер для PostgreSQL. ORM и сериализация данных в blob-поля не используются.
