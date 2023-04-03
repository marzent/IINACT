using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CactbotSelf.内存相关.offset
{
	internal unsafe partial class Offsets
	{
		public IntPtr _libraryHandle;
		public static string _tempfilename;
		private readonly object _librarylock = new object();
		public uint dataLength;
		public IntPtr Start;
		public static UInt64 camera;
		public static UInt64 MarkingController;
		public Offsets(string path)
		{
			_tempfilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) + ".exe";
			//using (File.Create(_tempfilename)) { 
			//}
			System.IO.File.Copy(path, _tempfilename, true);
			_libraryHandle = NativeMethods.LoadLibraryW(_tempfilename);
			if (_libraryHandle == IntPtr.Zero)
			{
				return;
			}

			NativeMethods.MODULEINFO info = new NativeMethods.MODULEINFO();
			if (_libraryHandle == IntPtr.Zero)
			{
				return;
			}
			if (!NativeMethods.GetModuleInformation(Process.GetCurrentProcess().Handle, _libraryHandle, out info, (uint)sizeof(NativeMethods.MODULEINFO)))
			{
				return;
			}
			Start = info.lpBaseOfDll;
			dataLength = info.SizeOfImage;
			camera = ScanText("48 8D 0D ?? ?? ?? ?? 45 33 C0 41 8D 51 02");
			MarkingController = ScanText("48 8B 94 24 ? ? ? ? 48 8D 0D ? ? ? ? 41 B0 01");

			//FieldInfo[] opcodeFieldInfos = typeof(SignatureManager).GetFields();
			//Type type = typeof(SignatureManager);
			//object obj1 = type.Assembly.CreateInstance("SignatureManager");
			//foreach (var opcode in opcodeFieldInfos)
			//{
			//    if (opcode.Name== "_vtableSignatures")
			//    {

			//    }
			//    opcode.SetValue(obj1, _vtableSignatures);
			//}

		}
		public void UnInitialize()
		{
			try
			{
				lock (_librarylock)
				{

					if (_libraryHandle != IntPtr.Zero)
					{
						NativeMethods.FreeLibrary(_libraryHandle);
						bool freed = NativeMethods.FreeLibrary(_libraryHandle);
						if (!freed)
							Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: {nameof(NativeMethods.FreeLibrary)} failed.", "DEBUG-MACHINA");
						_libraryHandle = IntPtr.Zero;

					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: exception in {nameof(UnInitialize)}: {ex}", "DEBUG-MACHINA");
			}
		}

		public UInt64 GetStaticAddressFromSig(string signature, int offset = 0)
		{
			var Addr = ScanText(signature);
			Addr = Addr + (UInt64)offset;
			var wantAdress = IntPtr.Add(Start, (int)Addr);
			long num;
			IntPtr instrAddr;
			do
			{
				instrAddr = IntPtr.Add(wantAdress, 1);
				num = ((Int32*)instrAddr)[0] + (long)instrAddr + 4 - (long)Start;
			}
			while (!(num >= (long)Start && num <= ((long)Start + dataLength)));
			return (ulong)IntPtr.Add(instrAddr, ((Int32*)instrAddr)[0] + 4);
		}
		public UInt64 ScanText(string pattern, int offect = 0)
		{
			var results = FindPattern(pattern);
			if (results.Count == 0)
				return default;

			var scanRet = results[0];
			var insnByte = ReadByte((ulong)scanRet + (ulong)offect);
			if (insnByte == 0xE8 || insnByte == 0xE9)
				return ReadCallSig(scanRet);
			//获取lea赋值的
			var insnByte1 = ReadByte(scanRet); var insnByte2 = ReadByte(scanRet + 1);
			if (insnByte1 == 0x48 && insnByte2 == 0x8D)
				return ReadCallSig1(scanRet);
			return scanRet;
		}
		private UInt64 ReadCallSig1(UInt64 sigLocation)
		{
			var offect = (sigLocation + 3);
			var wantAdress = IntPtr.Add(Start, (int)offect);
			var vcd = ((byte*)Start)[offect];
			var jumpOffset = ((Int32*)(wantAdress))[0];
			return sigLocation + 7 + (UInt64)jumpOffset;
		}
		private UInt64 ReadCallSig(UInt64 sigLocation)
		{
			var wantAdress = IntPtr.Add(Start, (int)sigLocation);
			var jumpOffset = ((Int32*)wantAdress)[0];
			return sigLocation + 5 + (UInt64)jumpOffset;
		}
		private UInt64 ReadCallSig2(UInt64 sigLocation)
		{
			var wantAdress = IntPtr.Add(Start, (int)sigLocation + 1);
			var jumpOffset = ((Int32*)wantAdress)[0];
			return sigLocation + 5 + (UInt64)jumpOffset;
		}
		public byte ReadByte(ulong scanRet)
		{
			var a = ((byte*)Start)[scanRet];
			return a;
		}
		public List<UInt64> FindPattern(string pattern)
		{
			var results = Find(HexToBytes(pattern), dataLength);
			return results;
		}
		List<UInt64> Find(List<int> pattern, uint dataLength)
		{

			List<UInt64> ret = new List<UInt64>();
			uint plen = (uint)pattern.Count;
			for (var i = 0; i < dataLength; i++)
			{
				if (ByteMatch(i, pattern))
					ret.Add((UInt64)i);
			}

			return ret;
		}
		bool ByteMatch(int start, List<int> pattern)
		{
			for (int i = start, j = 0; j < pattern.Count; i++, j++)
			{
				if (pattern[j] == -1)
					continue;
				var a = ((byte*)Start)[i];
				if (a != pattern[j])
					return false;
			}
			return true;
		}
		List<int> HexToBytes(string hex)
		{
			List<int> bytes = new List<int>();

			for (int i = 0; i < hex.Length - 1;)
			{
				if (hex[i] == '?')
				{
					if (hex[i + 1] == '?')
						i++;
					i++;
					bytes.Add(-1);
					continue;
				}
				if (hex[i] == ' ')
				{
					i++;
					continue;
				}

				string byteString = hex.Substring(i, 2);
				var _byte = byte.Parse(byteString, NumberStyles.AllowHexSpecifier);
				bytes.Add(_byte);
				i += 2;
			}

			return bytes;
		}
	}
}
