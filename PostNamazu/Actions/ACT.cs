using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Logfile;
using Newtonsoft.Json;
using PostNamazu.Attributes;
using PostNamazu.Common;
using PostNamazu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PostNamazu.Actions
{
	class Act
	{
		public int type { get; set; }
		public string text { get; set; }
	}
    internal class ACT:NamazuModule
    {
		public override void GetOffsets()
		{
			base.GetOffsets();

		}
		[Command("act")]
		public void DoMarking(string command)
		{
			var abc = command;

			var actLog = JsonConvert.DeserializeObject<Act>(command);
			var timestamp = DateTime.Now;
			WriteLogLineImpl(actLog.type, timestamp, actLog.text);
		}
		private ILogOutput _logOutput;
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal bool WriteLogLineImpl(int ID, DateTime timestamp, string line)
		{
			if (_logOutput == null)
			{
				var plugin = GetPluginData();
				_logOutput = (ILogOutput)plugin._iocContainer.GetService(typeof(ILogOutput));
			}
			_logOutput?.WriteLine((FFXIV_ACT_Plugin.Logfile.LogMessageType)ID, timestamp, line);
			return true;
		}
		private FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetPluginData()
		{
			return ActGlobals.oFormActMain.FfxivPlugin;
		}
	}
}
