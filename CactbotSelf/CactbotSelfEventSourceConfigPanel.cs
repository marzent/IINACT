using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CactbotSelf
{
	[Serializable]
	public class CactbotSelfEventSourceConfig
	{
		public string ExampleString = "Example String";
		public bool open;
		public List<string> shunxu = new List<string> { "黑骑", "枪刃", "战士", "骑士", "白魔", "占星", "贤者", "学者", "武士", "武僧", "镰刀", "龙骑", "忍者", "机工", "舞者", "诗人", "黑魔", "召唤", "赤魔" };
		public CactbotSelfEventSourceConfig()
		{

		}

		public static CactbotSelfEventSourceConfig LoadConfig(IPluginConfig pluginConfig)
		{
			var result = new CactbotSelfEventSourceConfig();
			if (pluginConfig.EventSourceConfigs.ContainsKey("CactbotSelfConfig"))
			{
				var obj = pluginConfig.EventSourceConfigs["CactbotSelfConfig"];
				if (obj.TryGetValue("ExampleString", out JToken value))
				{
					result.ExampleString = value.ToString();
				}
				if (obj.TryGetValue("shunxu", out JToken shunxu))
				{
					result.shunxu = shunxu.Values<string>().ToList();

				}
				if (obj.TryGetValue("open", out JToken open))
				{
					if (open.ToString() == "False")
					{
						result.open = false;
					}
					else
					{
						result.open = true;
					}
				}
			}
			return result;
		}

		public void SaveConfig(IPluginConfig pluginConfig)
		{
			pluginConfig.EventSourceConfigs["CactbotSelfConfig"] = JObject.FromObject(this);
		}
	}
}
