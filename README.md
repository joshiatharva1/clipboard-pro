# ClipBoard Pro

ClipBoard Pro is a Windows desktop helper for quickly filling repetitive job application fields. It keeps frequently used details such as your name, email, phone number, address, links, and other reusable text snippets in a compact always-on-top window so you can paste them into browser forms without retyping the same information again and again.

## Why I Built This

When applying to many jobs, most application forms ask for the same details repeatedly. Filling those fields manually across different websites is slow and frustrating. ClipBoard Pro solves that by keeping your commonly used values beside the browser and pasting the selected value into the active form field.

## Features

- Compact WPF desktop application built with C# and .NET 8.
- Always-on-top mode so the app can stay beside the browser while filling forms.
- Add, edit, save, delete, and reorder reusable text fields.
- Dedicated paste button for each saved value.
- Drag handle for rearranging fields based on personal workflow.
- Prevents saving blank values.
- Custom application/window icon.
- Stores values locally on the user's machine.

## Tech Stack

- C#
- .NET 8
- WPF
- Windows Forms interop for paste shortcuts
- Win32 APIs for window focus and keyboard input handling

## Getting Started

### Prerequisites

Install the .NET 8 SDK from Microsoft:

[Download .NET](https://dotnet.microsoft.com/download)

### Run From Source

Open a terminal in the project folder and run:

```powershell
dotnet run --project .\ClipBoardPro\ClipBoardPro.csproj
```

If `dotnet` is not recognized, use the full installed path:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' run --project .\ClipBoardPro\ClipBoardPro.csproj
```

### Build The App

```powershell
dotnet build .\ClipBoardPro\ClipBoardPro.csproj
```

The debug executable is generated at:

```text
ClipBoardPro\bin\Debug\net8.0-windows\ClipBoard Pro.exe
```

## How To Use

1. Open ClipBoard Pro beside your browser.
2. Click inside the browser form field where you want to insert text.
3. Click the paste button beside the saved value in ClipBoard Pro.
4. Use the `+` button to add new values.
5. Use the edit icon to modify a value, then click the tick icon to save it.
6. Use the delete icon to remove a saved value.
7. Use the drag handle to reorder fields.

## Project Structure

```text
ClipBoard Pro/
├── README.md
├── .gitignore
└── ClipBoardPro/
    ├── Assets/
    │   ├── clipboard.ico
    │   └── clipboard.png
    ├── Models/
    ├── Services/
    ├── App.xaml
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    └── ClipBoardPro.csproj
```

## Privacy Note

ClipBoard Pro is designed as a local desktop utility. The saved values are stored on the local machine and are not sent to any external server by the application.

## Future Improvements

- Add import/export support for saved fields.
- Add optional categories for grouping values.
- Add a packaged installer for easier installation.
- Add keyboard shortcuts for faster pasting.

## License

This project is currently intended for personal use. Add a license before distributing it publicly.


