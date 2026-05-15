using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace JobFillHelper.Services;

public sealed class WindowPasteService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly int _currentProcessId = Environment.ProcessId;
    private IntPtr _mainWindowHandle;
    private IntPtr _lastExternalWindow;
    private int _lastSendInputError;

    public WindowPasteService()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timer.Tick += (_, _) => CaptureForegroundWindow();
    }

    public void Start(Window window)
    {
        _mainWindowHandle = new WindowInteropHelper(window).Handle;
        _timer.Start();
    }

    public async Task<string> PasteTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Nothing to paste.";
        }

        var valueToPaste = text.Trim();
        var target = _lastExternalWindow;
        if (target == IntPtr.Zero || !IsWindow(target))
        {
            return "No browser/form window was captured yet.";
        }

        if (!IsExternalWindow(GetForegroundWindow()))
        {
            BringTargetToForeground(target);
            await Task.Delay(120);
        }

        if (!TrySetClipboardText(valueToPaste))
        {
            ReleaseModifierKeys();
            var typedInputs = TypeText(valueToPaste);
            ReleaseModifierKeys();
            return typedInputs > 0
                ? $"Typed {valueToPaste.Length} characters into the focused field."
                : $"Could not update clipboard or type text. Direct typing error was {_lastSendInputError}.";
        }

        try
        {
            ReleaseModifierKeys();
            Forms.SendKeys.SendWait("^v");
            ReleaseModifierKeys();
            await Task.Delay(120);
            return "Pasted current field.";
        }
        catch (Exception exception)
        {
            var sendKeysError = exception.GetType().Name;
            var sentInputs = SendCtrlV();
            ReleaseModifierKeys();
            await Task.Delay(120);
            return sentInputs == 4
                ? "Pasted current field with fallback shortcut."
                : $"Paste blocked. SendKeys error {sendKeysError}; shortcut inputs {sentInputs}/4, error {_lastSendInputError}.";
        }
        finally
        {
            await Task.Delay(180);
            ClearClipboardIfItStillContains(valueToPaste);
        }
    }
    public void CopyText(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            TrySetClipboardText(text);
        }
    }

    private void CaptureForegroundWindow()
    {
        var foregroundWindow = GetForegroundWindow();
        if (!IsExternalWindow(foregroundWindow))
        {
            return;
        }

        _lastExternalWindow = foregroundWindow;
    }

    private bool IsExternalWindow(IntPtr window)
    {
        if (window == IntPtr.Zero || window == _mainWindowHandle)
        {
            return false;
        }

        GetWindowThreadProcessId(window, out var processId);
        return processId != _currentProcessId;
    }

    private uint SendInputs(Input[] inputs)
    {
        _lastSendInputError = 0;
        var sentInputs = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sentInputs != inputs.Length)
        {
            _lastSendInputError = Marshal.GetLastWin32Error();
        }

        return sentInputs;
    }

    private uint SendCtrlV()
    {
        var inputs = new[]
        {
            CreateKeyboardInput(VirtualKeys.Control, 0),
            CreateKeyboardInput(VirtualKeys.V, 0),
            CreateKeyboardInput(VirtualKeys.V, KeyEventFlags.KeyUp),
            CreateKeyboardInput(VirtualKeys.Control, KeyEventFlags.KeyUp)
        };

        return SendInputs(inputs);
    }

    private void ReleaseModifierKeys()
    {
        var inputs = new[]
        {
            CreateKeyboardInput(VirtualKeys.Control, KeyEventFlags.KeyUp),
            CreateKeyboardInput(VirtualKeys.LeftControl, KeyEventFlags.KeyUp),
            CreateKeyboardInput(VirtualKeys.RightControl, KeyEventFlags.KeyUp),
            CreateKeyboardInput(VirtualKeys.Shift, KeyEventFlags.KeyUp),
            CreateKeyboardInput(VirtualKeys.Alt, KeyEventFlags.KeyUp)
        };

        SendInputs(inputs);
    }

    private uint TypeText(string text)
    {
        var normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var totalSent = 0u;

        foreach (var character in normalizedText)
        {
            var inputCharacter = character == '\n' ? '\r' : character;
            var inputs = new[]
            {
                CreateUnicodeInput(inputCharacter, 0),
                CreateUnicodeInput(inputCharacter, KeyEventFlags.KeyUp)
            };

            var sent = SendInputs(inputs);
            totalSent += sent;

            if (sent != inputs.Length)
            {
                break;
            }

            Thread.Sleep(3);
        }

        return totalSent;
    }

    private static bool TrySetClipboardText(string text)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(text, System.Windows.TextDataFormat.UnicodeText);

                if (System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText) &&
                    System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.UnicodeText) == text)
                {
                    return true;
                }
            }
            catch (ExternalException)
            {
                Thread.Sleep(50);
            }

            Thread.Sleep(25);
        }

        return false;
    }

    private static void ClearClipboardIfItStillContains(string text)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText) &&
                    System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.UnicodeText) == text)
                {
                    System.Windows.Clipboard.Clear();
                }

                return;
            }
            catch (ExternalException)
            {
                Thread.Sleep(40);
            }
        }
    }
    private static IntPtr GetFocusedWindow(IntPtr fallbackWindow)
    {
        var targetThreadId = GetWindowThreadProcessId(fallbackWindow, out _);
        if (targetThreadId == 0)
        {
            return fallbackWindow;
        }

        var guiThreadInfo = new GuiThreadInfo
        {
            Size = Marshal.SizeOf<GuiThreadInfo>()
        };

        return GetGUIThreadInfo(targetThreadId, ref guiThreadInfo) && guiThreadInfo.Focus != IntPtr.Zero
            ? guiThreadInfo.Focus
            : fallbackWindow;
    }

    private static void BringTargetToForeground(IntPtr target)
    {
        if (IsIconic(target))
        {
            ShowWindow(target, ShowWindowCommands.Restore);
        }

        var currentThreadId = GetCurrentThreadId();
        var targetThreadId = GetWindowThreadProcessId(target, out _);

        if (targetThreadId != 0 && targetThreadId != currentThreadId)
        {
            AttachThreadInput(currentThreadId, targetThreadId, true);
            try
            {
                BringWindowToTop(target);
                SetForegroundWindow(target);
                SetFocus(target);
            }
            finally
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }

            return;
        }

        BringWindowToTop(target);
        SetForegroundWindow(target);
        SetFocus(target);
    }

    private static Input CreateKeyboardInput(ushort virtualKey, KeyEventFlags flags)
    {
        return new Input
        {
            Type = InputType.Keyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = virtualKey,
                    ScanCode = 0,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static Input CreateUnicodeInput(char character, KeyEventFlags flags)
    {
        return new Input
        {
            Type = InputType.Keyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = 0,
                    ScanCode = character,
                    Flags = flags | KeyEventFlags.Unicode,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    public void Dispose()
    {
        _timer.Stop();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo guiThreadInfo);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands command);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInputStructure);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    private static class WindowMessages
    {
        public const uint Paste = 0x0302;
    }

    private enum ShowWindowCommands
    {
        Restore = 9
    }

    private enum InputType : uint
    {
        Keyboard = 1
    }

    [Flags]
    private enum KeyEventFlags : uint
    {
        KeyUp = 0x0002,
        Unicode = 0x0004
    }

    private static class VirtualKeys
    {
        public const ushort Control = 0x11;
        public const ushort LeftControl = 0xA2;
        public const ushort RightControl = 0xA3;
        public const ushort Shift = 0x10;
        public const ushort Alt = 0x12;
        public const ushort V = 0x56;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public InputType Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public KeyEventFlags Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size;
        public uint Flags;
        public IntPtr Active;
        public IntPtr Focus;
        public IntPtr Capture;
        public IntPtr MenuOwner;
        public IntPtr MoveSize;
        public IntPtr Caret;
        public Rect CaretRect;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
