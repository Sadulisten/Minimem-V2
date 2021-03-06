﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Minimem.Features
{
	public class Reader
	{
		private readonly Main _mainReference;

		public Reader(Main main)
		{
			_mainReference = main ?? throw new Exception($"Parameter \"main\" for constructor of Features.Reader cannot be null");
		}

		public T Read<T>(IntPtr address) where T : struct
		{
			if (_mainReference.ProcessHandle == IntPtr.Zero) throw new Exception("Read/Write Handle cannot be Zero");
			if (_mainReference == null) throw new Exception("Reference to Main Class cannot be null");
			if (!_mainReference.IsValid) throw new Exception("Reference to Main Class reported an Invalid State");
#if x86
			byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
			bool flag = Win32.PInvoke.ReadProcessMemory(_mainReference.ProcessHandle, address, buffer, Marshal.SizeOf(typeof(T)), out IntPtr numBytesRead);
#else
			byte[] buffer = new byte[(long)Marshal.SizeOf(typeof(T))];
			bool flag = Win32.PInvoke.ReadProcessMemory(_mainReference.ProcessHandle, address, buffer, (long)Marshal.SizeOf(typeof(T)), out IntPtr numBytesRead);
#endif
			return HelperMethods.ByteArrayToStructure<T>(buffer);
		}
		public Task<T> AsyncRead<T>(IntPtr address) where T : struct
		{
			return Task.Run(() => Read<T>(address));
		}

		public string ReadString(IntPtr address, Encoding encoding, int maxLength = 128, bool zeroTerminated = false)
		{
			if (_mainReference.ProcessHandle == IntPtr.Zero) throw new Exception("Read/Write Handle cannot be Zero");
			if (_mainReference == null) throw new Exception("Reference to Main Class cannot be null");
			if (!_mainReference.IsValid) throw new Exception("Reference to Main Class reported an Invalid State");
			if (encoding == null) encoding = Encoding.UTF8;
			var buff = new byte[maxLength];
#if x86
			bool flag = Win32.PInvoke.ReadProcessMemory(_mainReference.ProcessHandle, address, buff, buff.Length, out IntPtr numBytesRead);
#else
			bool flag = Win32.PInvoke.ReadProcessMemory(_mainReference.ProcessHandle, address, buff, buff.LongLength, out IntPtr numBytesRead);
#endif
			return zeroTerminated ? encoding.GetString(buff).Split('\0')[0] : encoding.GetString(buff);
		}
		public Task<string> AsyncReadString(IntPtr address, Encoding encoding, int maxLength = 128, bool zeroTerminated = false)
		{
			return Task.Run(() => ReadString(address, encoding, maxLength, zeroTerminated));
		}

		public byte[] ReadBytes(IntPtr address, IntPtr size, Classes.MemoryProtection overrideProtectionType = Classes.MemoryProtection.DoNothing)
		{
			if (_mainReference.ProcessHandle == IntPtr.Zero) throw new Exception("Read/Write Handle cannot be Zero");
			if (_mainReference == null) throw new Exception("Reference to Main Class cannot be null");
			if (!_mainReference.IsValid) throw new Exception("Reference to Main Class reported an Invalid State");
			if (address == IntPtr.Zero || size == IntPtr.Zero) return null;

			Classes.MemoryProtection oldProtect = Classes.MemoryProtection.Invalid;
			if (overrideProtectionType != Classes.MemoryProtection.DoNothing && overrideProtectionType != Classes.MemoryProtection.Invalid)
			{
#if x86
				bool success = Win32.PInvoke.VirtualProtectEx(_mainReference.ProcessHandle, address, size, overrideProtectionType, out oldProtect);
#else
				bool success = Win32.PInvoke.VirtualProtectEx(_mainReference.ProcessHandle, address, size, overrideProtectionType, out oldProtect);
#endif
			}

#if x86
			byte[] buffer = new byte[size.ToInt32()];
			Win32.PInvoke.ReadProcessMemory(_mainReference.ProcessHandle, address, buffer, buffer.Length, out IntPtr numBytesRead);
#else
			byte[] buffer = new byte[size.ToInt64()];
			Win32.PInvoke.ReadProcessMemory(_mainReference.ProcessHandle, address, buffer, buffer.LongLength, out IntPtr numBytesRead);
#endif

			if (oldProtect != Classes.MemoryProtection.Invalid)
				Win32.PInvoke.VirtualProtectEx(_mainReference.ProcessHandle, address, new IntPtr(buffer.LongLength), oldProtect, out oldProtect);
			return buffer;
		}
		public Task<byte[]> AsyncReadBytes(IntPtr address, IntPtr size, Classes.MemoryProtection overrideProtectionType = Classes.MemoryProtection.DoNothing)
		{
			return Task.Run(() => ReadBytes(address, size, overrideProtectionType));
		}

		public T[] ReadArray<T>(IntPtr address, IntPtr count) where T : struct
		{
			if (_mainReference.ProcessHandle == IntPtr.Zero) throw new Exception("Read/Write Handle cannot be Zero");
			if (_mainReference == null) throw new Exception("Reference to Main Class cannot be null");
			if (!_mainReference.IsValid) throw new Exception("Reference to Main Class reported an Invalid State");
			if (address == IntPtr.Zero || count == IntPtr.Zero || (int) count < 0) return null;

			int itemSize = Marshal.SizeOf(typeof(T));
#if x86
			int totalSize = itemSize * count.ToInt32();
#else
			long totalSize = itemSize * count.ToInt64();
#endif
			List<byte> buff = _mainReference.Reader.ReadBytes(address, new IntPtr(totalSize)).ToList();
			if (buff.Count < 1) return null;
			List<T> returnArray = new List<T>();

			for (int idx = 0; idx < buff.Count; idx += itemSize)
			{
				returnArray.Add(HelperMethods.ByteArrayToStructure<T>(buff.Skip(idx).Take(itemSize).ToArray()));
			}

			return returnArray.ToArray();
		}
		public Task<T[]> AsyncReadArray<T>(IntPtr address, IntPtr count) where T : struct
		{
			return Task.Run(() => ReadArray<T>(address, count));
		}
	}
}
