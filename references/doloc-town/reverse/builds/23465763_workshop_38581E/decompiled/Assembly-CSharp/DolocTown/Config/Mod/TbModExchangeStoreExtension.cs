using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModExchangeStoreExtension
{
	private readonly List<ModExchangeStoreExtensionInfo> _dataList;

	public List<ModExchangeStoreExtensionInfo> DataList => _dataList;

	public TbModExchangeStoreExtension(JSONNode _json)
	{
		_dataList = new List<ModExchangeStoreExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModExchangeStoreExtensionInfo item = ModExchangeStoreExtensionInfo.DeserializeModExchangeStoreExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModExchangeStoreExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModExchangeStoreExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
