using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModDishGroupExtension
{
	private readonly List<ModDishGroupExtensionInfo> _dataList;

	public List<ModDishGroupExtensionInfo> DataList => _dataList;

	public TbModDishGroupExtension(JSONNode _json)
	{
		_dataList = new List<ModDishGroupExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModDishGroupExtensionInfo item = ModDishGroupExtensionInfo.DeserializeModDishGroupExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModDishGroupExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModDishGroupExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
