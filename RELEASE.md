# Инструкция по сборке и релизу

## Требования

- Windows 10/11
- .NET 8.0 SDK
- PowerShell 5.1+

## Сборка

### Автоматическая сборка

```powershell
.\build.ps1
```

Скрипт:
1. Очищает папку `dist/`
2. Восстанавливает NuGet-пакеты
3. Публикует приложение как single-file portable exe
4. Копирует документацию в `dist/`

### Ручная сборка

```powershell
# Восстановить зависимости
dotnet restore src/NativeMDView.csproj

# Собрать релизную версию
dotnet publish src/NativeMDView.csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:PublishReadyToRunComposite=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o dist
```

## Результат сборки

После сборки в папке `dist/` будет:

| Файл | Размер | Описание |
|------|--------|----------|
| `MDView.exe` | ~73 MB | Портативный exe, всё включено |
| `readme.md` | ~2 KB | Документация |
| `release.md` | ~1 KB | Эта инструкция |

## Настройки

При первом запуске рядом с `MDView.exe` создаётся `settings.ini`:

```ini
[General]
Theme = dark
ShowToolbar = True
Zoom = 1

[Window]
Width = 1000
Height = 700
Left = 100
Top = 100
Maximized = True

[View]
Mode = preview
```

## Зависимости

| Пакет | Версия | Назначение |
|-------|--------|------------|
| Markdig | 0.37.0 | Парсинг Markdown |
| ini-parser-netstandard | 2.5.2 | Чтение/запись INI-файлов |

## Структура проекта

```
src/
├── NativeMDView.csproj    # Проект
├── App.xaml               # Точка входа
├── App.xaml.cs
├── MainWindow.xaml        # Главное окно
├── MainWindow.xaml.cs
├── MarkdownRenderer.cs    # Рендерер Markdown → WPF
├── Settings.cs            # Настройки (INI)
├── LinkDialog.xaml        # Диалог вставки ссылки
├── LinkDialog.xaml.cs
├── ImageDialog.xaml       # Диалог вставки изображения
├── ImageDialog.xaml.cs
└── app.ico                # Иконка приложения
```

## Портативность

Приложение полностью портативное:
- Все зависимости встроены в exe (single-file publish)
- .NET Runtime включён в сборку
- Настройки хранятся в `settings.ini` рядом с exe
- Не требует установки WebView2 Runtime
- Не пишет в реестр или AppData
