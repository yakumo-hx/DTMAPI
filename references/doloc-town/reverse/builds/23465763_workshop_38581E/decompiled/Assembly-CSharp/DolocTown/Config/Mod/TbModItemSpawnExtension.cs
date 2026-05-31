using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModItemSpawnExtension
{
	private readonly List<ModItemSpawnExtensionInfo> _dataList;

	public List<ModItemSpawnExtensionInfo> DataList => _dataList;

	public TbModItemSpawnExtension(JSONNode _json)
	{
		_dataList = new List<ModItemSpawnExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModItemSpawnExtensionInfo item = ModItemSpawnExtensionInfo.DeserializeModItemSpawnExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModItemSpawnExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModItemSpawnExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
