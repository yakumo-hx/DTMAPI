using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModImageSetting
{
	private readonly List<ModImageSettingInfo> _dataList;

	public List<ModImageSettingInfo> DataList => _dataList;

	public TbModImageSetting(JSONNode _json)
	{
		_dataList = new List<ModImageSettingInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModImageSettingInfo item = ModImageSettingInfo.DeserializeModImageSettingInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModImageSettingInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModImageSettingInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
