using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModResourceSpawnExtension
{
	private readonly List<ModResourceSpawnExtensionInfo> _dataList;

	public List<ModResourceSpawnExtensionInfo> DataList => _dataList;

	public TbModResourceSpawnExtension(JSONNode _json)
	{
		_dataList = new List<ModResourceSpawnExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModResourceSpawnExtensionInfo item = ModResourceSpawnExtensionInfo.DeserializeModResourceSpawnExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModResourceSpawnExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModResourceSpawnExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
