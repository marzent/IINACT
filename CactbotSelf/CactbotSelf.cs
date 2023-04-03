using Advanced_Combat_Tracker;
using CactbotSelf.内存相关;
using CactbotSelf.内存相关.offset;
using RainbowMage.OverlayPlugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CactbotSelf
{
	public class CactbotSelf
	{
		public string pluginPath = "";
		private static TinyIoCContainer TinyIoCContainer;
		private static Registry Registry;
		private static EventSource EventSource;
		private BackgroundWorker _processSwitcher;
		public static Process FFXIV;
		public static FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivPlugin;
		public static MainClass mainClass;
		public static CactbotSelf cactbotSelf;
		public TabPage tabPagetabPagetabPage;
		public List<string> ShunXu = new();
		public bool usePostNanazu = false;

		public void DeInitPlugin()
		{
			//mainClass.DeInitPlugin();

			if (System.IO.File.Exists(Offsets._tempfilename))
			{
				System.IO.File.Delete(Offsets._tempfilename);
			}
			foreach (var item in Registry.EventSources)
			{
				if (item.Name == "CactbotSelf" && item != null)
				{
					item.Dispose();
				}

			}
			Type type = typeof(Registry);
			FieldInfo fieldInfo = type.GetField("_eventSources", BindingFlags.Instance | BindingFlags.NonPublic);
			((List<IEventSource>)fieldInfo.GetValue(Registry)).Remove(EventSource);
		}
		public CactbotSelf(List<string> shunxu,bool PostNanazu)
		{
			ShunXu = shunxu;
			usePostNanazu = PostNanazu;
			cactbotSelf = this;
		}
		public void ChangeSetting(List<string> shunxu, bool PostNanazu) 
		{
			ShunXu = shunxu;
			usePostNanazu = PostNanazu;
		}
		public void Init()
		{
			//if (TinyIoCContainer is not null)
			//{
			//	return;
			//}
			//获取sig
			var window = NativeMethods.FindWindow("FFXIVGAME", null);
			NativeMethods.GetWindowThreadProcessId(window, out var pid);
			var proc = Process.GetProcessById(Convert.ToInt32(pid));
			TinyIoCContainer = Registry.GetContainer();

			Registry = TinyIoCContainer.Resolve<Registry>();
			var gamePath = proc.MainModule?.FileName;

			var oodleNative_Ffxiv = new Offsets(gamePath);

			oodleNative_Ffxiv.findNetDown();
			oodleNative_Ffxiv.UnInitialize();
			//mainClass = new MainClass();
			//mainClass.InitPlugin(PluginUI);
			//var container = Registry.GetContainer();
			//var registry = container.Resolve<Registry>();
			//var eventSource = (EventSource)registry.EventSources.FirstOrDefault(p => p.Name == "CactbotSelf");
			//if (eventSource == null)
			//{
			//	eventSource = new EventSource(container);
			//	registry.StartEventSource(eventSource);
			//}

		}
		public object GetFfxivPlugin()
		{
			var plugin = ActGlobals.oFormActMain.FfxivPlugin;
			return (FFXIV_ACT_Plugin.FFXIV_ACT_Plugin)plugin ?? throw new Exception("找不到FFXIV解析插件，请确保其加载顺序位于鲶鱼精邮差之前。");
		}

		public void InitPlugin()
		{

			GetFfxivPlugin();
			// We don't need a tab here.
			var selfPluginData = Assembly.GetExecutingAssembly().Location;
			pluginPath = Path.GetDirectoryName(selfPluginData);
			TinyIoCContainer = Registry.GetContainer(); ;
			Registry = TinyIoCContainer.Resolve<Registry>();
			if (EventSource == null)
			{
				EventSource = new EventSource(TinyIoCContainer);
				Registry.StartEventSource(EventSource);
			}
			//FFXIV = ffxivPlugin.DataRepository.GetCurrentFFXIVProcess()
			//?? Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault();
			//之前办法
			//         _processSwitcher = new BackgroundWorker { WorkerSupportsCancellation = true };
			//_processSwitcher.DoWork += ProcessSwitcher;
			//_processSwitcher.RunWorkerAsync();


			Init();
		}
		/// <summary>
		///     代替ProcessChanged委托，手动循环检测当前活动进程并进行注入。
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

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
		private Process GetFFXIVProcess()
		{
			return ffxivPlugin.DataRepository.GetCurrentFFXIVProcess();
		}

	}
}

