using CactbotSelf.内存相关.offset;
using CactbotSelf.内存相关;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Advanced_Combat_Tracker;
using RainbowMage.OverlayPlugin.EventSources;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin;


namespace CactbotSelf
{
	public class EventSource : EventSourceBase, IDisposable
	{
		public MemHelper _memhelper;
		private System.Timers.Timer fast_update_timer_;
		private static int kFastTimerMilli = 80;
		private static int kSlowTimerMilli = 300;
		public delegate void PlayerControlHandler(JSEvents.PlayerControlEvent e);
		public event PlayerControlHandler OnPlayerControl;
		public delegate void GetConfigHandler(JSEvents.GetConfigEvent e);
		public event GetConfigHandler GetConfig;
		public IntPtr cameraAdress;
		public IntPtr MarkingAdress;
		protected ILogger logger;
		private BackgroundWorker _processSwitcher;
		public FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivPlugin;
		public Process FFXIV;
		public CactbotSelfEventSourceConfig Config { get; private set; }
		public long DataSectionOffset { get; private set; }
		/// <summary>
		/// The size of the .data section.
		/// </summary>
		public int DataSectionSize { get; private set; }
		GameCamera data { get; set; }
		public EventSource(TinyIoCContainer container) : base(container)
		{
			RegisterEventTypes(new List<string>()
	  {
		"onPlayerControl",
		"getConfig",
	  });
			logger = container.Resolve<ILogger>();

			Name = "CactbotSelf";
			//InitializeEvents();


			RegisterEventHandler("getConfig", (msg) =>
			{
				var send = new JSEvents.GetConfigEvent();
				send.isOpen = CactbotSelf.cactbotSelf.usePostNanazu;
				var list = new List<JSEvents.ShunXu>();
				for (int i = 0; i < CactbotSelf.cactbotSelf.ShunXu.Count; i++)
				{
					var shunxu = new JSEvents.ShunXu(i, CactbotSelf.cactbotSelf.ShunXu[i]);
					list.Add(shunxu);
				}
				send.shunxu = list;
				return JObject.FromObject(send);
			}
			);

		}
		public object GetFfxivPlugin()
		{
			ffxivPlugin = null;

			if (ffxivPlugin == null)
			{
				var plugin = ActGlobals.oFormActMain.FfxivPlugin;
				return (FFXIV_ACT_Plugin.FFXIV_ACT_Plugin)plugin ?? throw new Exception("找不到FFXIV解析插件，请确保其加载顺序位于鲶鱼精邮差之前。");
			}
			return ffxivPlugin;
		}
		private void SetupSearchSpace(ProcessModule module)
		{
			var baseAddress = module.BaseAddress;

			// We don't want to read all of IMAGE_DOS_HEADER or IMAGE_NT_HEADER stuff so we cheat here.
			var ntNewOffset = ReadInt32(baseAddress, 0x3C);
			var ntHeader = baseAddress + ntNewOffset;

			// IMAGE_NT_HEADER
			var fileHeader = ntHeader + 4;
			var numSections = ReadInt16(ntHeader, 6);

			// IMAGE_OPTIONAL_HEADER
			var optionalHeader = fileHeader + 20;

			IntPtr sectionHeader;
			sectionHeader = optionalHeader + 240;

			// IMAGE_SECTION_HEADER
			var sectionCursor = sectionHeader;
			for (var i = 0; i < numSections; i++)
			{
				var sectionName = ReadInt64(sectionCursor);

				// .text
				switch (sectionName)
				{
					case 0x617461642E: // .data
						DataSectionOffset = ReadInt32(sectionCursor, 12);
						DataSectionSize = ReadInt32(sectionCursor, 8);
						break;
				}

				sectionCursor += 40;
			}
		}
		public unsafe GameCamera getGameCamera(byte[] source)
		{
			fixed (byte* p = source)
			{
				GameCamera mem = *(GameCamera*)&p[0];
				return mem;
			}

		}
		public IntPtr GetStaticAddressFromSig(IntPtr instrAddr, int offset = 0)
		{
			instrAddr = IntPtr.Add(instrAddr, offset);
			long bAddr = (long)_memhelper.BaseAddress;
			long num;
			do
			{
				instrAddr = IntPtr.Add(instrAddr, 1);
				num = ReadInt32(instrAddr) + (long)instrAddr + 4 - bAddr;
			}
			while (!(num >= DataSectionOffset && num <= DataSectionOffset + DataSectionSize));
			return IntPtr.Add(instrAddr, ReadInt32(instrAddr) + 4);
		}
		public void DispatchToJS(JSEvent e)
		{
			JObject ev = new JObject();
			ev["type"] = e.EventName();
			ev["detail"] = JObject.FromObject(e);
			DispatchEvent(ev);
		}
		private void InitializeEvents()
		{

		}
		public Byte ReadByte(IntPtr address, int offset = 0) => _memhelper.Read<Byte>(IntPtr.Add(address, offset));
		public Int16 ReadInt16(IntPtr address, int offset = 0) => _memhelper.Read<Int16>(IntPtr.Add(address, offset));
		public Int32 ReadInt32(IntPtr address, int offset = 0) => _memhelper.Read<Int32>(IntPtr.Add(address, offset));
		public Int64 ReadInt64(IntPtr address, int offset = 0) => _memhelper.Read<Int64>(IntPtr.Add(address, offset));
		public float ReadFloat(IntPtr address, int offset = 0) => _memhelper.Read<float>(IntPtr.Add(address, offset));
		public IntPtr ReadIntPtr(IntPtr address, int offset = 0) => _memhelper.Read<IntPtr>(IntPtr.Add(address, offset));


		public override void LoadConfig(IPluginConfig config)
		{
			Config = CactbotSelfEventSourceConfig.LoadConfig(config);
		}

		public override void SaveConfig(IPluginConfig config)
		{
			Config.SaveConfig(config);
		}
		public override void Stop()
		{
			fast_update_timer_.Stop();

		}
		public override void Start()
		{
			GetFfxivPlugin();
			_processSwitcher = new BackgroundWorker { WorkerSupportsCancellation = true };
			_processSwitcher.DoWork += ProcessSwitcher;
			_processSwitcher.RunWorkerAsync();


		}
		private void ProcessSwitcher(object sender, DoWorkEventArgs e)
		{
			while (true)
			{
				if (_processSwitcher.CancellationPending)
				{
					e.Cancel = true;
					break;
				}
				FFXIV = GetFFXIVProcess();
				if (FFXIV != null)
				{
					Init();
					return;
				}
				//if (FFXIV != GetFFXIVProcess())
				//{
				//	FFXIV = GetFFXIVProcess();
				//	if (FFXIV is not null)
				//		if (FFXIV.ProcessName == "ffxiv")
				//			MessageBox.Show("错误：游戏运行于DX9模式下") ;
				//		else Init();
				//}

				Thread.Sleep(3000);
			}
		}
		private static TinyIoCContainer TinyIoCContainer;
		private static Registry Registry;
		public MainClass mainClass;
		public void Init()
		{
			//获取sig;
			var window = NativeMethods.FindWindow("FFXIVGAME", null);
			NativeMethods.GetWindowThreadProcessId(window, out var pid);
			var proc = Process.GetProcessById(Convert.ToInt32(pid));
			var gamePath = proc.MainModule?.FileName;
			//var oodleNative_Ffxiv = new Offsets(gamePath);
			_memhelper = new MemHelper(FFXIV);
			SetupSearchSpace(FFXIV.MainModule);
			IntPtr cameraOffect = (IntPtr)((UInt64)_memhelper.BaseAddress + Offsets.camera);
			cameraAdress = ReadIntPtr(cameraOffect);
			var abc = Offsets.MarkingController + (ulong)_memhelper.BaseAddress;
			MarkingAdress = IntPtr.Add(GetStaticAddressFromSig((IntPtr)abc), 0x1b0);
			//oodleNative_Ffxiv.findNetDown();
			//oodleNative_Ffxiv.UnInitialize();
			mainClass = new MainClass();
			mainClass.InitPlugin();

			fast_update_timer_ = new System.Timers.Timer();
			fast_update_timer_.Elapsed += (o, args) =>
			{
				int timer_interval = kSlowTimerMilli;
				try
				{
					timer_interval = SendFastRateEvents();
				}
				catch (Exception e)
				{
					// SendFastRateEvents holds this semaphore until it exits.
				}
				fast_update_timer_.Interval = timer_interval;
			};
			fast_update_timer_.AutoReset = false;
			OnPlayerControl += (e) => DispatchToJS(e);
			GetConfig += (e) => DispatchToJS(e);
			fast_update_timer_.Interval = kFastTimerMilli;
			fast_update_timer_.Start();

		}
		private Process GetFFXIVProcess()
		{
			return Process.GetProcessesByName("ffxiv_dx11")[0];
		}
		protected override void Update()
		{

		}
		Waymark ReadWaymark(IntPtr addr, WaymarkID id) => new Waymark
		{
			X = ReadFloat(addr),
			Y = ReadFloat(addr + 0x4),
			Z = ReadFloat(addr + 0x8),
			Active = ReadByte(addr + 0x1C) == 1,
			ID = id
		};
		private unsafe int SendFastRateEvents()
		{
			//byte[] source = _memhelper.ReadBytes(cameraAdress, GameCamera.Size);
			//data = getGameCamera(source);
			//var caream = data.CurrentHRotation;
			var tempMarks = new WayMarks();

			tempMarks.A = ReadWaymark(MarkingAdress + 0x00, WaymarkID.A);
			tempMarks.B = ReadWaymark(MarkingAdress + 0x20, WaymarkID.B);
			tempMarks.C = ReadWaymark(MarkingAdress + 0x40, WaymarkID.C);
			tempMarks.D = ReadWaymark(MarkingAdress + 0x60, WaymarkID.D);
			tempMarks.One = ReadWaymark(MarkingAdress + 0x80, WaymarkID.One);
			tempMarks.Two = ReadWaymark(MarkingAdress + 0xA0, WaymarkID.Two);
			tempMarks.Three = ReadWaymark(MarkingAdress + 0xC0, WaymarkID.Three);
			tempMarks.Four = ReadWaymark(MarkingAdress + 0xE0, WaymarkID.Four);
			//JSEvents.Camera caream = new JSEvents.Camera((long)data.VTable, data.X, data.Z, data.Y, data.CurrentZoom, data.MinZoom, data.MaxZoom, data.CurrentFoV, data.MinFoV, data.MaxFoV,data.AddedFoV,data.CurrentHRotation,data.CurrentVRotation,data.MinVRotation,data.MaxVRotation,data.Tilt,data.Mode,data.LookAtHeightOffset,data.ResetLookatHeightOffset,data.Z2);
			JSEvents.Camera caream = new JSEvents.Camera(ReadFloat(cameraAdress + 0x130), ReadFloat(cameraAdress + 0x134));
			OnPlayerControl(new JSEvents.PlayerControlEvent(caream, tempMarks.A, tempMarks.B, tempMarks.C, tempMarks.D, tempMarks.One, tempMarks.Two, tempMarks.Three, tempMarks.Four));
			var window = NativeMethods.FindWindow("FFXIVGAME", null);
			bool game_active = window != IntPtr.Zero ? true : false;
			return game_active ? kFastTimerMilli : kSlowTimerMilli;
		}

	}
}
