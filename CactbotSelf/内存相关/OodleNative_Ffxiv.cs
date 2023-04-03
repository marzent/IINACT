using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CactbotSelf.内存相关
{
	public class OodleNative_Ffxiv
	{
		private readonly object _librarylock = new object();
		public Dictionary<SignatureType, int> _offsets { get; set; }
		public string _tempfilename;
		public IntPtr _libraryHandle;
		public void Initialize(string path)
		{
			try
			{
				if (!System.IO.File.Exists(path))
				{
					return;
				}
				lock (_librarylock)
				{
					//var _libraryTempPath = System.IO.Path.GetTempFileName();

					_tempfilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) + ".exe";
					//using (File.Create(_tempfilename)) { 
					//}
					System.IO.File.Copy(path, _tempfilename, true);
					_libraryHandle = NativeMethods.LoadLibraryW(_tempfilename);
					if (_libraryHandle == IntPtr.Zero)
					{
						return;
					}
					_offsets = new SigScan().Read(_libraryHandle);

				}

			}
			catch (Exception)
			{

				throw;
			}
		}
		public void UnInitialize()
		{
			try
			{
				lock (_librarylock)
				{

					if (_libraryHandle != IntPtr.Zero)
					{
						bool freed = NativeMethods.FreeLibrary(_libraryHandle);
						if (!freed)
							Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: {nameof(NativeMethods.FreeLibrary)} failed.", "DEBUG-MACHINA");
						_libraryHandle = IntPtr.Zero;
						if (System.IO.File.Exists(_tempfilename))
						{
							System.IO.File.Delete(_tempfilename);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: exception in {nameof(UnInitialize)}: {ex}", "DEBUG-MACHINA");
			}
		}
	}
}
