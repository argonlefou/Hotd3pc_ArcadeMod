﻿using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Hotd3Arcade_Launcher
{
    public class Codecave
    {
        private Process _Process;
        private IntPtr _ProcessHandle;
        private IntPtr _ModuleBaseAddress = IntPtr.Zero;
        private UInt32 _Cave_Address = 0;
        private UInt32 _CaveOffset = 0;

        public UInt32 CaveAddress
        {
            get { return _Cave_Address; }
        }
        public UInt32 CaveOffset
        {
            get { return _CaveOffset; }
        }

        public Codecave(Process p, IntPtr BaseAddress)
        {
            _Process = p;
            _ModuleBaseAddress = BaseAddress;
        }

        /// <summary>
        /// Trying to access the process
        /// </summary>
        /// <returns>True if success, otherwise False</returns>
        public bool Open()
        {
            _ProcessHandle = _Process.Handle;
            if (_ProcessHandle != IntPtr.Zero)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Reserves a region of memory within the virtual address space of a specified process. 
        /// The function initializes the memory it allocates to zero.
        /// </summary>
        /// <param name="Size">The size of the region of memory to allocate, in bytes.</param>
        /// <returns>True is success, otherwise False</returns>
        public bool Alloc(UInt32 Size)
        {
            //Allocation mémoire
            _Cave_Address = (UInt32)Win32API.VirtualAllocEx(_ProcessHandle, IntPtr.Zero, Size, MemoryAllocType.MEM_COMMIT, MemoryPageProtect.PAGE_EXECUTE_READWRITE);
            if (_Cave_Address != 0)
                return true;
            else
                return false;
        }

        //call Address
        public bool Write_call(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 5;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0xE8);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //cmp eax,[Value]
        public bool Write_cmp(int Value)
        {
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x81);
            Buffer.Add(0xF9);
            Buffer.AddRange(BitConverter.GetBytes(Value));
            return Write_Bytes(Buffer.ToArray());
        }

        //je [Address]
        public bool Write_je(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x84);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jb [Address]
        public bool Write_jb(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x82);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jng [Address]
        public bool Write_jng(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8E);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jnl [Address]
        public bool Write_jnl(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8D);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jng [Address]
        public bool Write_jg(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8F);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jng [Address]
        public bool Write_ja(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x87);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jnl [Address]
        public bool Write_jl(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8C);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jmp [Address]
        public bool Write_jmp(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 5;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //nop
        public bool Write_nop(int Amount = 1)
        {
            List<Byte> Buffer = new List<byte>();
            for (int i = 0; i < Amount; i++)
            {
                Buffer.Add(0x90);
            }
            return Write_Bytes(Buffer.ToArray());
        }

        /// <summary>
        /// Write bytes in memory, read from a string like "00 00 00 00"
        /// </summary>
        /// <param name="StrBuffer">String formated series of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public bool Write_StrBytes(String StrBuffer)
        {
            String[] sBytes = StrBuffer.Split(' ');
            List<Byte> Buffer = new List<byte>();
            foreach (String hex in sBytes)
            {
                Buffer.Add((byte)Convert.ToInt32(hex, 16));
            }
            return Write_Bytes(Buffer.ToArray());
        }


        /// <summary>
        /// Write bytes in memory, read from an array of bytes
        /// </summary>
        /// <param name="Buffer">Array of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public bool Write_Byte(Byte Data)
        {
            UInt32 BytesWritten = 0;
            if (Win32API.WriteProcessMemory(_ProcessHandle, _Cave_Address + _CaveOffset, new byte[]{Data}, 1, ref BytesWritten))
            {
                _CaveOffset += BytesWritten;
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Write bytes in memory, read from an array of bytes
        /// </summary>
        /// <param name="Buffer">Array of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public bool Write_Bytes(Byte[] Buffer)
        {
            UInt32 BytesWritten = 0;
            if (Win32API.WriteProcessMemory(_ProcessHandle, _Cave_Address + _CaveOffset, Buffer, (UInt32)Buffer.Length, ref BytesWritten))
            {
                _CaveOffset += BytesWritten;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
