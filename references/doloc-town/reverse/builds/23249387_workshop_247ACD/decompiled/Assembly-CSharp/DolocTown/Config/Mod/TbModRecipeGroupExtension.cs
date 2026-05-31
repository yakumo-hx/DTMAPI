using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModRecipeGroupExtension
{
	private readonly List<ModRecipeGroupExtensionInfo> _dataList;

	public List<ModRecipeGroupExtensionInfo> DataList => _dataList;

	public TbModRecipeGroupExtension(JSONNode _json)
	{
		_dataList = new List<ModRecipeGroupExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModRecipeGroupExtensionInfo item = ModRecipeGroupExtensionInfo.DeserializeModRecipeGroupExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModRecipeGroupExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModRecipeGroupExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
