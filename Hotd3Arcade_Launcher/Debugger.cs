using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debugger;

namespace Debugger
{
    #region DEBUG_EVENT

    [StructLayout(LayoutKind.Sequential)]
    public struct DEBUG_EVENT
    {
        public DebugEventType dwDebugEventCode;
        public int dwProcessId;
        public int dwThreadId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
        byte[] debugInfo;

        public EXCEPTION_DEBUG_INFO Exception
        {
            get { return GetDebugInfo<EXCEPTION_DEBUG_INFO>(); }
        }

        public CREATE_THREAD_DEBUG_INFO CreateThread
        {
            get { return GetDebugInfo<CREATE_THREAD_DEBUG_INFO>(); }
        }

        public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo
        {
            get { return GetDebugInfo<CREATE_PROCESS_DEBUG_INFO>(); }
        }

        public EXIT_THREAD_DEBUG_INFO ExitThread
        {
            get { return GetDebugInfo<EXIT_THREAD_DEBUG_INFO>(); }
        }

        public EXIT_PROCESS_DEBUG_INFO ExitProcess
        {
            get { return GetDebugInfo<EXIT_PROCESS_DEBUG_INFO>(); }
        }

        public LOAD_DLL_DEBUG_INFO LoadDll
        {
            get { return GetDebugInfo<LOAD_DLL_DEBUG_INFO>(); }
        }

        public UNLOAD_DLL_DEBUG_INFO UnloadDll
        {
            get { return GetDebugInfo<UNLOAD_DLL_DEBUG_INFO>(); }
        }

        public OUTPUT_DEBUG_STRING_INFO DebugString
        {
            get { return GetDebugInfo<OUTPUT_DEBUG_STRING_INFO>(); }
        }

        public RIP_INFO RipInfo
        {
            get { return GetDebugInfo<RIP_INFO>(); }
        }

        private T GetDebugInfo<T>() where T : struct
        {
            var structSize = Marshal.SizeOf(typeof(T));
            var pointer = Marshal.AllocHGlobal(structSize);
            Marshal.Copy(debugInfo, 0, pointer, structSize);

            var result = Marshal.PtrToStructure(pointer, typeof(T));
            Marshal.FreeHGlobal(pointer);
            return (T)result;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct EXCEPTION_DEBUG_INFO
        {
            public EXCEPTION_RECORD ExceptionRecord;
            public uint dwFirstChance;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXCEPTION_RECORD
        {
            public uint ExceptionCode;
            public uint ExceptionFlags;
            public IntPtr ExceptionRecord;
            public IntPtr ExceptionAddress;
            public uint NumberParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15, ArraySubType = UnmanagedType.U4)]
            public uint[] ExceptionInformation;
        }

        public delegate uint PTHREAD_START_ROUTINE(IntPtr lpThreadParameter);

        [StructLayout(LayoutKind.Sequential)]
        public struct CREATE_THREAD_DEBUG_INFO
        {
            public IntPtr hThread;
            public IntPtr lpThreadLocalBase;
            public PTHREAD_START_ROUTINE lpStartAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CREATE_PROCESS_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr hProcess;
            public IntPtr hThread;
            public IntPtr lpBaseOfImage;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpThreadLocalBase;
            public PTHREAD_START_ROUTINE lpStartAddress;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXIT_THREAD_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXIT_PROCESS_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LOAD_DLL_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr lpBaseOfDll;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNLOAD_DLL_DEBUG_INFO
        {
            public IntPtr lpBaseOfDll;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OUTPUT_DEBUG_STRING_INFO
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpDebugStringData;
            public ushort fUnicode;
            public ushort nDebugStringLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RIP_INFO
        {
            public uint dwError;
            public uint dwType;
        }
    }

    #endregion

    public enum DebugEventType : uint
    {
        RIP_EVENT = 9,
        OUTPUT_DEBUG_STRING_EVENT = 8,
        UNLOAD_DLL_DEBUG_EVENT = 7,
        LOAD_DLL_DEBUG_EVENT = 6,
        EXIT_PROCESS_DEBUG_EVENT = 5,
        EXIT_THREAD_DEBUG_EVENT = 4,
        CREATE_PROCESS_DEBUG_EVENT = 3,
        CREATE_THREAD_DEBUG_EVENT = 2,
        EXCEPTION_DEBUG_EVENT = 1,
    }

    public enum ContinueStatus : uint
    {
        DBG_CONTINUE = 0x00010002,
        DBG_EXCEPTION_NOT_HANDLED = 0x80010001,
        DBG_REPLY_LATER = 0x40010001
    }

    public class QuickDebugger
    {
        #region Debugger WIN32 functions

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes,
           bool bInheritHandles,
           uint dwCreationFlags,
           IntPtr lpEnvironment,
           string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitForDebugEvent(ref DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, ContinueStatus dwContinueStatus);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcessStop(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UInt32 lpBaseAddress, byte[] lpBuffer, UInt32 dwSize, ref UInt32 lpNumberOfBytesWritten);

        #endregion        
               
        #region Win32 Struct

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        #endregion

        public const uint STATUS_BREAKPOINT = 0x80000003;
        
        const uint DEBUG_ONLY_THIS_PROCESS = 0x00000002;
        const UInt32 INFINITE = 0xffffffff;

        private String _TargetExePath;
        private DEBUG_EVENT _DebugEvent;
        private bool _ContinueDebugging = true;

        public delegate void DebugEventHandler(object sender, DebugEventArgs e);
        public event DebugEventHandler OnDebugEvent;

        public QuickDebugger(String PathToExe)
        {
            _TargetExePath = PathToExe;
        }
        
        public void StartProcess()
        {
            PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();
            STARTUPINFO sInfo = new STARTUPINFO();
            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);
            tSec.nLength = Marshal.SizeOf(tSec);

            CreateProcess(_TargetExePath, "", ref pSec, ref tSec, false, DEBUG_ONLY_THIS_PROCESS, IntPtr.Zero, null, ref sInfo, out pInfo);

            _DebugEvent = new DEBUG_EVENT();
            while (_ContinueDebugging)
            {
                if (!WaitForDebugEvent(ref _DebugEvent, INFINITE))
                    return;

                if (OnDebugEvent != null)
                    OnDebugEvent(this, new DebugEventArgs(_DebugEvent));
                else
                    ContinueDebugEvent();
            }
        }

        public void ContinueDebugEvent()
        {
            ContinueDebugEvent(_DebugEvent.dwProcessId, _DebugEvent.dwThreadId, ContinueStatus.DBG_CONTINUE);
        }

        public void StopDebuging()
        {
            _ContinueDebugging = false;
        }

        public void DetachDebugger()
        {
            DebugActiveProcessStop((uint)_DebugEvent.dwProcessId);
            _ContinueDebugging = false;
        }
    }

    public class DebugEventArgs : EventArgs
    {
        public DEBUG_EVENT Dbe { get; private set; }

        public DebugEventArgs(DEBUG_EVENT DebugEvent)
        {
            Dbe = DebugEvent;
        }
    }
}
