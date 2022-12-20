using System;
using System.Runtime.InteropServices;

namespace Hotd3Arcade_Launcher
{
    public static class Win32API
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UInt32 lpBaseAddress, byte[] lpBuffer, UInt32 dwSize, ref UInt32 lpNumberOfBytesRead);   

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UInt32 lpBaseAddress, byte[] lpBuffer, UInt32 dwSize, ref UInt32 lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, UInt32 dwSize, MemoryAllocType flAllocationType, MemoryPageProtect flProtect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
    }

    public static class Win32Define
    {
        //Mouse and Keyboard Hook
        public const int WH_MOUSE_LL = 14;
        public const int WH_KEYBOARD_LL = 13;

        //Windows messages
        public const UInt32 WM_COPYDATA = 0x004A;
        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYUP = 0x0101;
        public const UInt32 WM_MOUSEMOVE = 0x0200;
        public const UInt32 WM_LBUTTONDOWN = 0x0201;
        public const UInt32 WM_LBUTTONUP = 0x0202;
        public const UInt32 WM_INPUT = 0x00FF;
        public const UInt32 WM_RBUTTONDOWN = 0x0204;
        public const UInt32 WM_RBUTTONUP = 0x0205;
        public const UInt32 WM_MBUTTONDOWN = 0x0207;
        public const UInt32 WM_MBUTTONUP = 0x0208;
        public const UInt32 WM_MOUSEWHEEL = 0x020A;
        public const UInt32 WM_SYSKEYDOWN = 0x0104;


        public const uint MAPVK_VK_TO_VSC = 0x00;
        public const uint MAPVK_VSC_TO_VK = 0x01;
        public const uint MAPVK_VK_TO_CHAR = 0x02;
        public const uint MAPVK_VSC_TO_VK_EX = 0x03;
        public const uint MAPVK_VK_TO_VSC_EX = 0x04;
    }

    [Flags]
    public enum MemoryAllocType
    {
        MEM_COMMIT = 0x1000,
        MEM_RESERVE = 0x2000,
        MEM_RESET = 0x8000,
        MEM_RESET_UNDO = 0x1000000,
    }

    [Flags]
    public enum MemoryPageProtect
    {
        PAGE_EXECUTE = 0x10,
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_EXECUTE_WRITECOPY = 0x80,
        PAGE_NOACCESS = 0x01,
        PAGE_READONLY = 0x02,
        PAGE_READWRITE = 0x04,
        PAGE_WRITECOPY = 0x08,
        PAGE_TARGETS_INVALID = 0x40000000,
        PAGE_TARGETS_NO_UPDATE = 0x40000000
    }

    public enum HardwareScanCode : byte
    {
        DIK_ESCAPE = 0x01,
        DIK_1 = 0x02,
        DIK_2 = 0x03,
        DIK_3 = 0x04,
        DIK_4 = 0x05,
        DIK_5 = 0x06,
        DIK_6 = 0x07,
        DIK_7 = 0x08,
        DIK_8 = 0x09,
        DIK_9 = 0x0A,
        DIK_0 = 0x0B,
        DIK_MINUS = 0x0C,
        DIK_EQUALS = 0x0D,
        DIK_BACK = 0x0E,
        DIK_TAB = 0x0F,
        DIK_Q = 0x10,
        DIK_W = 0x11,
        DIK_E = 0x12,
        DIK_R = 0x13,
        DIK_T = 0x14,
        DIK_Y = 0x15,
        DIK_U = 0x16,
        DIK_I = 0x17,
        DIK_O = 0x18,
        DIK_P = 0x19,
        DIK_LBRACKET = 0x1A,
        DIK_RBRACKET = 0x1B,
        DIK_RETURN = 0x1C,
        DIK_LCONTROL = 0x1D,
        DIK_A = 0x1E,
        DIK_S = 0x1F,
        DIK_D = 0x20,
        DIK_F = 0x21,
        DIK_G = 0x22,
        DIK_H = 0x23,
        DIK_J = 0x24,
        DIK_K = 0x25,
        DIK_L = 0x26,
        DIK_SEMICOLON = 0x27,
        DIK_APOSTROPHE = 0x28,
        DIK_GRAVE = 0x29,
        DIK_LSHIFT = 0x2A,
        DIK_BACKSLASH = 0x2B,
        DIK_Z = 0x2C,
        DIK_X = 0x2D,
        DIK_C = 0x2E,
        DIK_V = 0x2F,
        DIK_B = 0x30,
        DIK_N = 0x31,
        DIK_M = 0x32,
        DIK_COMMA = 0x33,
        DIK_PERIOD = 0x34,
        DIK_SLASH = 0x35,
        DIK_RSHIFT = 0x36,
        DIK_MULTIPLY = 0x37,
        DIK_LMENU = 0x38,
        DIK_SPACE = 0x39,
        DIK_CAPITAL = 0x3A,
        DIK_F1 = 0x3B,
        DIK_F2 = 0x3C,
        DIK_F3 = 0x3D,
        DIK_F4 = 0x3E,
        DIK_F5 = 0x3F,
        DIK_F6 = 0x40,
        DIK_F7 = 0x41,
        DIK_F8 = 0x42,
        DIK_F9 = 0x43,
        DIK_F10 = 0x44,
        DIK_NUMLOCK = 0x45,
        DIK_SCROLL = 0x46,
        DIK_NUMPAD7 = 0x47,
        DIK_NUMPAD8 = 0x48,
        DIK_NUMPAD9 = 0x49,
        DIK_SUBTRACT = 0x4A,
        DIK_NUMPAD4 = 0x4B,
        DIK_NUMPAD5 = 0x4C,
        DIK_NUMPAD6 = 0x4D,
        DIK_ADD = 0x4E,
        DIK_NUMPAD1 = 0x4F,
        DIK_NUMPAD2 = 0x50,
        DIK_NUMPAD3 = 0x51,
        DIK_NUMPAD0 = 0x52,
        DIK_DECIMAL = 0x53,
        DIK_F11 = 0x57,
        DIK_F12 = 0x58,
        DIK_F13 = 0x64,
        DIK_F14 = 0x65,
        DIK_F15 = 0x66,
        DIK_KANA = 0x70,
        DIK_CONVERT = 0x79,
        DIK_NOCONVERT = 0x7B,
        DIK_YEN = 0x7D,
        DIK_NUMPADEQUALS = 0x8D,
        DIK_CIRCUMFLEX = 0x90,
        DIK_AT = 0x91,
        DIK_COLON = 0x92,
        DIK_UNDERLINE = 0x93,
        DIK_KANJI = 0x94,
        DIK_STOP = 0x95,
        DIK_AX = 0x96,
        DIK_UNLABELED = 0x97,
        DIK_NUMPADENTER = 0x9C,
        DIK_RCONTROL = 0x9D,
        DIK_NUMPADCOMMA = 0xB3,
        DIK_DIVIDE = 0xB5,
        DIK_SYSRQ = 0xB7,
        DIK_RMENU = 0xB8,
        DIK_HOME = 0xC7,
        DIK_UP = 0xC8,
        DIK_PRIOR = 0xC9,
        DIK_LEFT = 0xCB,
        DIK_RIGHT = 0xCD,
        DIK_END = 0xCF,
        DIK_DOWN = 0xD0,
        DIK_NEXT = 0xD1,
        DIK_INSERT = 0xD2,
        DIK_DELETE = 0xD3,
        DIK_LWIN = 0xDB,
        DIK_RWIN = 0xDC,
        DIK_APPS = 0xDD,
    }

    /// <summary>
    /// Contains information about a low-level keyboard input event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        /// <summary>
        /// A virtual-key code. The code must be a value in the range 1 to 254.
        /// </summary>
        public int vkCode;
        /// <summary>
        /// A hardware scan code for the key.
        /// </summary>
        public HardwareScanCode scanCode;
        /// <summary>
        /// The extended-key flag, event-injected flags, context code, and transition-state flag. 
        /// </summary>
        public int flags;
        /// <summary>
        /// The time stamp for this message, equivalent to what GetMessageTime would return for this message.
        /// </summary>
        public int time;
        /// <summary>
        /// Additional information associated with the message.
        /// </summary>
        public UIntPtr dwExtraInfo;

        public override string ToString()
        {
            return string.Format("vkCode=0x{0:X}, scanCode=0x{1:X}, flags={2}, time={3}, dwextrainfo={4:X}",
                                                vkCode, scanCode, flags, time, dwExtraInfo);
        }
    }
}
