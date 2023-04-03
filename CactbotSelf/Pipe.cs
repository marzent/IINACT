using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using H.Pipes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CactbotSelf
{
	internal class Pipe
	{
		public Pipe() 
		{
			InitPipeClient();

		}
		static PipeClient<string> pipeClient;
		static Process FFXIV;
		private static BackgroundWorker _processSwitcher;
		private  void InitPipeClient()
		{
			var pipeName = $"DDD";
			pipeClient = new PipeClient<string>(pipeName);
			RestartPipeClient();
		}
		private ILogOutput _logOutput;
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal bool WriteLogLineImpl(int ID,string line)
		{
			if (_logOutput == null)
			{
				var plugin = GetPluginData();
				_logOutput = (ILogOutput)plugin._iocContainer.GetService(typeof(ILogOutput));
			}
			var timestamp = DateTime.Now;
			_logOutput?.WriteLine((FFXIV_ACT_Plugin.Logfile.LogMessageType)ID, timestamp, line);
			return true;
		}
		private FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetPluginData()
		{
			return ActGlobals.oFormActMain.FfxivPlugin;
		}
		private  void RestartPipeClient()
		{
			try
			{
				var process = Process.GetProcessesByName("ffxiv_dx11")[0];
				var pipeName = $"DDD{process.Id}";
				pipeClient = new PipeClient<string>(pipeName);
				pipeClient.Connected += (o, args) =>
				{
					
				};
				pipeClient.MessageReceived += (o, args) =>
				{
					try
					{
						string[] array = args.Message.Split(new char[]
{
					'#'
});
						
                        if (array.Length>=2)
                        {
							var type = Convert.ToInt32(array[0]);
							var text = array[1];
							if (type==11)
							{
								var plugin = GetPluginData();
								var date=(DataSubscription)plugin._iocContainer.GetService(typeof(DataSubscription));
								var partys = text.Split(new char[] {'|'});
								if (partys.Length>0) 
								{
									var size = Convert.ToInt32(partys[0]);
									var list=new List<uint>();
									for (int i = 1; i < partys.Length; i++)
									{
										list.Add((uint)Convert.ToInt32(partys[i],16));
									}

									date.OnPartyListChanged(list.AsReadOnly(),size);
								}
								
								
							}
							else
							{
								WriteLogLineImpl(type, text);
							}
							
						}
                    }
					catch (Exception ex)
					{
						MessageBox.Show($"传入消息错误：{ex.Message}");
					}

					//Log("200", args.Message);
				};
				pipeClient.Disconnected += (o, args) =>
				{
					pipeClient.ConnectAsync();
				};
				pipeClient.ConnectAsync();

			}
			catch (Exception ex)
			{
				MessageBox.Show($"{ex.Message}");

			}

		}
	}
}
