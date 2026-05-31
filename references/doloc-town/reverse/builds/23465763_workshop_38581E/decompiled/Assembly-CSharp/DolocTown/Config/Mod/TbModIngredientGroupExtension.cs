using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModIngredientGroupExtension
{
	private readonly List<ModIngredientGroupExtensionInfo> _dataList;

	public List<ModIngredientGroupExtensionInfo> DataList => _dataList;

	public TbModIngredientGroupExtension(JSONNode _json)
	{
		_dataList = new List<ModIngredientGroupExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModIngredientGroupExtensionInfo item = ModIngredientGroupExtensionInfo.DeserializeModIngredientGroupExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModIngredientGroupExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModIngredientGroupExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
