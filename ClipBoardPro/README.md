# JobFill Helper

Small Windows desktop helper for repetitive job application fields.

## What it does

- Keeps reusable values such as name, phone, address, links, etc.
- Stays on top beside your browser.
- Copies any value with one click.
- Attempts one-click paste by returning focus to the last active non-app window and sending `Ctrl+V`.
- Saves fields locally to `%APPDATA%\JobFillHelper\fields.json`.

## Requirements

- Windows
- .NET 8 SDK or Visual Studio 2022 with the .NET desktop workload

## Run

```powershell
cd JobFillHelper
dotnet run
```

## Build an executable

```powershell
cd JobFillHelper
dotnet publish -c Release -r win-x64 --self-contained true
```

The generated app will be under `bin\Release\net8.0-windows\win-x64\publish`.

## Notes

Clicking the helper app normally steals focus from the browser, so the app watches the previously active external window. When you press `Paste`, it copies the value, restores that previous window, and sends a paste keystroke.

Avoid storing highly sensitive values such as SSNs or passwords in this first version. Encryption and timed clipboard clearing are good next steps.
