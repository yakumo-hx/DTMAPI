using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModFishingPoolExtension
{
	private readonly List<ModFishingPoolExtensionInfo> _dataList;

	public List<ModFishingPoolExtensionInfo> DataList => _dataList;

	public TbModFishingPoolExtension(JSONNode _json)
	{
		_dataList = new List<ModFishingPoolExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModFishingPoolExtensionInfo item = ModFishingPoolExtensionInfo.DeserializeModFishingPoolExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModFishingPoolExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModFishingPoolExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
