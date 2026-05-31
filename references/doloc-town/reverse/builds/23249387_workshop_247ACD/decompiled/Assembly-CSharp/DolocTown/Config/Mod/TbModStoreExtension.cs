using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModStoreExtension
{
	private readonly List<ModStoreExtensionInfo> _dataList;

	public List<ModStoreExtensionInfo> DataList => _dataList;

	public TbModStoreExtension(JSONNode _json)
	{
		_dataList = new List<ModStoreExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModStoreExtensionInfo item = ModStoreExtensionInfo.DeserializeModStoreExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModStoreExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModStoreExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
