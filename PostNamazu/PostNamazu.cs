using Advanced_Combat_Tracker;
using Dalamud.Plugin.Services;
using PostNamazu.Attributes;
using PostNamazu.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace PostNamazu
{
	public class PostNamazu 
	{
        public static ICommandManager commandManager;

        public PostNamazu(ICommandManager com)
		{
			var process = Process.GetProcessesByName("ffxiv_dx11")[0];
            commandManager = com;

        }


		private BackgroundWorker _processSwitcher;

		private HttpServer _httpServer;
		private OverlayHoster.Program _overlayHoster;

		internal Process FFXIV;
		internal FFXIV_ACT_Plugin.FFXIV_ACT_Plugin FFXIV_ACT_Plugin;
		internal SigScanner SigScanner;


		private Dictionary<string, HandlerDelegate> CmdBind = new(StringComparer.OrdinalIgnoreCase); //key不区分大小写

		private List<NamazuModule> Modules = new();

		#region Init
		public void InitPlugin()
		{


			FFXIV_ACT_Plugin = GetFFXIVPlugin();

			_processSwitcher = new BackgroundWorker { WorkerSupportsCancellation = true };
			_processSwitcher.DoWork += ProcessSwitcher;
			_processSwitcher.RunWorkerAsync();
				ServerStart();

			InitializeActions();
			OverlayIntegration();


		}

		public void DeInitPlugin()
		{
			//FFXIV_ACT_Plugin.DataSubscription.ProcessChanged -= ProcessChanged;
			Detach();
			_overlayHoster.DeInit();
			if (_httpServer != null) ServerStop();
			_processSwitcher.CancelAsync();

		}



		public delegate void HandlerDelegate(string command);

		/// <summary>
		///     注册命令
		/// </summary>
		public void InitializeActions()
		{
			foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(NamazuModule)) && !t.IsAbstract))
			{
#if DEBUG
				
#endif
				var module = (NamazuModule)Activator.CreateInstance(t);
				module.Init(this);
				Modules.Add(module);
			
				var commands = module.GetType().GetMethods().Where(method => method.GetCustomAttributes<CommandAttribute>().Any());
				foreach (var action in commands)
				{
					var handlerDelegate = (HandlerDelegate)Delegate.CreateDelegate(typeof(HandlerDelegate), module, action);
					foreach (var command in action.GetCustomAttributes<CommandAttribute>())
					{
						SetAction(command.Command, handlerDelegate);
#if DEBUG
					
#endif
					}

				}
			}
		}

		private void ServerStart(object sender = null, EventArgs e = null)
		{
			try
			{
				_httpServer = new HttpServer(2019);
				_httpServer.PostNamazuDelegate = DoAction;
				_httpServer.OnException += OnException;


			}
			catch (Exception ex)
			{
				OnException(ex);
			}
		}

		private void ServerStop(object sender = null, EventArgs e = null)
		{
			_httpServer.Stop();
			_httpServer.PostNamazuDelegate = null;
			_httpServer.OnException -= OnException;


		}

		/// <summary>
		/// 委托给HttpServer类的异常处理
		/// </summary>
		/// <param name="ex"></param>
		private void OnException(Exception ex)
		{
			string errorMessage = $"无法在{_httpServer.Port}端口启动监听\n{ex.Message}";


			MessageBox.Show(errorMessage);
		}


		/// <summary>
		///     对当前解析插件对应的游戏进程进行注入
		/// </summary>
		private unsafe void Attach()
		{
			try
			{

				//Memory = new ExternalProcessMemory(FFXIV, true, false, _entrancePtr, false, 5, true);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"注入进程时发生错误！\n{ex}", "鲶鱼精邮差", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Detach();
			}
			foreach (var m in Modules)
			{
				m.Setup();
			}
		}

		/// <summary>
		///     解除注入
		/// </summary>
		private void Detach()
		{
			try
			{

					
			}
			catch (Exception)
			{
				// ignored
			}
		}

		/// <summary>
		///     取得解析插件
		/// </summary>
		/// <returns></returns>
		private FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetFFXIVPlugin()
		{
			var plugin = ActGlobals.oFormActMain.FfxivPlugin;
			return (FFXIV_ACT_Plugin.FFXIV_ACT_Plugin)plugin ?? throw new Exception("找不到FFXIV解析插件，请确保其加载顺序位于鲶鱼精邮差之前。");
		}

		/// <summary>
		///     取得解析插件对应的游戏进程
		/// </summary>
		/// <returns>解析插件当前对应进程</returns>
		private Process GetFFXIVProcess()
		{
			return FFXIV_ACT_Plugin.DataRepository.GetCurrentFFXIVProcess();
		}

		/// <summary>
		///     获取几个重要的地址
		/// </summary>
		/// <returns>返回是否成功找到入口地址</returns>
		private bool GetOffsets()
		{
			SigScanner = new SigScanner(FFXIV);
			try
			{
				
				return true;
			}
			catch (ArgumentOutOfRangeException)
			{
				//PluginUI.Log("无法对当前进程注入\n可能是已经被其他进程注入了");
			}

			try
			{
			
				return true;
			}
			catch (ArgumentOutOfRangeException)
			{

			}
			return false;
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

				if (FFXIV != GetFFXIVProcess())
				{
					Detach();
					FFXIV = GetFFXIVProcess();
					if (FFXIV != null)
						
						 if (GetOffsets())
							Attach();
				}
				Thread.Sleep(3000);
			}
		}


		/// <summary>
		/// OverlayPlugin集成
		/// </summary>
		private void OverlayIntegration()
		{
			try
			{
				var plugin = ActGlobals.oFormActMain.FfxivPlugin;
				if (plugin == null)
				{
		
					return;
				}

				_overlayHoster = new OverlayHoster.Program { PostNamazuDelegate = DoAction };
				_overlayHoster.Init();
			}
			catch (Exception ex)
			{
		
			}
		}

		/// <summary>
		///     解析插件对应进程改变时触发，解除当前注入并注入新的游戏进程
		///     目前由于解析插件的bug，ProcessChanged事件无法正常触发，暂时弃用。
		/// </summary>
		/// <param name="tProcess"></param>
		[Obsolete]
		private void ProcessChanged(Process tProcess)
		{
			if (tProcess.Id != FFXIV?.Id)
			{
				Detach();
				FFXIV = tProcess;
				if (FFXIV != null)
					if (GetOffsets())
						Attach();
		
			}
		}

		/// <summary>
		///     AssemblyResolve事件的处理函数，该函数用来自定义程序集加载逻辑
		///     GrayMagic也打包了，已经不需要再从外部加载dll了
		/// </summary>
		/// <param name="sender">事件引发源</param>
		/// <param name="args">事件参数，从该参数中可以获取加载失败的程序集的名称</param>
		/// <returns></returns>
		[Obsolete]
		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = args.Name.Split(',')[0];
			//if (name != "GreyMagic") return null;
			switch (name)
			{
				case "GreyMagic":
					var selfPluginData = Assembly.GetExecutingAssembly().Location;
					var path = Path.GetDirectoryName(selfPluginData);
					return Assembly.LoadFile($@"{path}\{name}.dll");
				default:
					return null;
			}
		}
		#endregion

		#region Delegate

		/// <summary>
		///     执行指令对应的方法
		/// </summary>
		/// <param name="command"></param>
		/// <param name="payload"></param>
		public void DoAction(string command, string payload)
		{
			try
			{
				string reflectedType = GetAction(command).GetMethodInfo().ReflectedType!.Name;

				
					GetAction(command)(payload);

				
			}
			catch (Exception ex)
			{
			
			}
		}

		/// <summary>
		///     设置指令与对应的方法
		/// </summary>
		/// <param name="command">指令类型</param>
		/// <param name="action">对应指令的方法委托</param>
		public void SetAction(string command, HandlerDelegate action)
		{
			CmdBind[command] = action;
		}

		/// <summary>
		///     获取指令对应的方法
		/// </summary>
		/// <param name="command">指令类型</param>
		/// <returns>对应指令的委托方法</returns>
		private HandlerDelegate GetAction(string command)
		{
			try
			{
				return CmdBind[command];
			}
			catch
			{
				throw new Exception($@"不支持的操作{command}");
			}
		}

		/// <summary>
		///     清空绑定的委托列表
		/// </summary>
		public void ClearAction()
		{
			CmdBind.Clear();
		}
		#endregion
	}
}
