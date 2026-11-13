using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace PhoneMouse.Server;

internal static class NativeInput
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private static void SendMouse(uint flags, int dx = 0, int dy = 0, uint data = 0)
    {
        var input = new INPUT {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dx = dx, dy = dy, dwFlags = flags, mouseData = data } }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public static void Move(double dx, double dy) =>
        SendMouse(MOUSEEVENTF_MOVE, (int)Math.Round(dx), (int)Math.Round(dy));

    public static void Click(string? button)
    {
        var flags = (button ?? "left").ToLowerInvariant() switch
        {
            "right" => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
            "middle" => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
            _ => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP)
        };
        SendMouse(flags.Item1);
        SendMouse(flags.Item2);
    }

    public static void Scroll(int delta) => SendMouse(MOUSEEVENTF_WHEEL, data: unchecked((uint)delta));

    public static void MouseDown(string? button) => SendMouse(ButtonFlags(button).Item1);
    public static void MouseUp(string? button) => SendMouse(ButtonFlags(button).Item2);
    public static void DoubleClick(string? button)
    {
        Click(button);
        Click(button);
    }
    private static (uint, uint) ButtonFlags(string? button) =>
        (button ?? "left").ToLowerInvariant() switch
        {
            "right" => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
            "middle" => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
            _ => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP)
        };

    private static MMDevice GetDefaultAudioDevice()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public static (float Level, bool Muted) GetVolume()
    {
        using var device = GetDefaultAudioDevice();
        return (device.AudioEndpointVolume.MasterVolumeLevelScalar, device.AudioEndpointVolume.Mute);
    }

    public static void SetVolume(float level)
    {
        using var device = GetDefaultAudioDevice();
        device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(level, 0f, 1f);
    }

    public static void ToggleMute()
    {
        using var device = GetDefaultAudioDevice();
        device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
    }

    public static void Zoom(int delta)
    {
        var down = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0x11 } } };
        var wheel = new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { mouseData = unchecked((uint)delta), dwFlags = MOUSEEVENTF_WHEEL } } };
        var up = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0x11, dwFlags = KEYEVENTF_KEYUP } } };
        var inputs = new[] { down, wheel, up };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void TypeText(string text)
    {
        foreach (var ch in text)
        {
            var down = new INPUT {
                type = INPUT_KEYBOARD,
                U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE } }
            };
            var up = new INPUT {
                type = INPUT_KEYBOARD,
                U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } }
            };
            SendInput(1, new[] { down }, Marshal.SizeOf<INPUT>());
            SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
        }
    }

    public static void PressKey(string key)
    {
        ushort vk = key.ToLowerInvariant() switch
        {
            "enter" => 0x0D, "backspace" => 0x08, "tab" => 0x09, "escape" => 0x1B,
            "space" => 0x20, "up" => 0x26, "down" => 0x28, "left" => 0x25, "right" => 0x27,
            "home" => 0x24, "end" => 0x23, "pageup" => 0x21, "pagedown" => 0x22,
            "insert" => 0x2D, "delete" => 0x2E,
            "f1" => 0x70, "f2" => 0x71, "f3" => 0x72, "f4" => 0x73,
            "f5" => 0x74, "f6" => 0x75, "f7" => 0x76, "f8" => 0x77,
            "f9" => 0x78, "f10" => 0x79, "f11" => 0x7A, "f12" => 0x7B,
            "volumeup" => 0xAF, "volumedown" => 0xAE, "volumemute" => 0xAD,
            _ => (ushort)0
        };
        if (vk == 0) return;
        var down = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk } } };
        var up = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } } };
        SendInput(1, new[] { down }, Marshal.SizeOf<INPUT>());
        SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
    }

    public static void PressHotkey(string[] keys)
    {
        var codes = keys.Select(KeyCode).Where(v => v != 0).Distinct().ToArray();
        if (codes.Length == 0 || codes.Length > 4) return;
        var inputs = new List<INPUT>();
        foreach (var code in codes)
            inputs.Add(new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = code } } });
        foreach (var code in codes.Reverse())
            inputs.Add(new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = code, dwFlags = KEYEVENTF_KEYUP } } });
        SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    private static ushort KeyCode(string key) => key.ToLowerInvariant() switch
    {
        "ctrl" or "control" => 0x11, "alt" => 0x12, "shift" => 0x10, "win" => 0x5B,
        "enter" => 0x0D, "tab" => 0x09, "escape" or "esc" => 0x1B, "space" => 0x20,
        "backspace" => 0x08, "delete" => 0x2E,
        "up" => 0x26, "down" => 0x28, "left" => 0x25, "right" => 0x27,
        "home" => 0x24, "end" => 0x23, "pageup" => 0x21, "pagedown" => 0x22,
        "+" => 0xBB, "-" => 0xBD, "a" => 0x41, "b" => 0x42, "c" => 0x43, "d" => 0x44, "e" => 0x45,
        "f" => 0x46, "g" => 0x47, "h" => 0x48, "i" => 0x49, "j" => 0x4A,
        "k" => 0x4B, "l" => 0x4C, "m" => 0x4D, "n" => 0x4E, "o" => 0x4F,
        "p" => 0x50, "q" => 0x51, "r" => 0x52, "s" => 0x53, "t" => 0x54,
        "u" => 0x55, "v" => 0x56, "w" => 0x57, "x" => 0x58, "y" => 0x59, "z" => 0x5A,
        _ => (ushort)0
    };
}