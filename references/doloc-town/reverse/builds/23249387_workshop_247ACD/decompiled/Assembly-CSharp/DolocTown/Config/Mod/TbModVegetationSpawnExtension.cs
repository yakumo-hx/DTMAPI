using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class TbModVegetationSpawnExtension
{
	private readonly List<ModVegetationSpawnExtensionInfo> _dataList;

	public List<ModVegetationSpawnExtensionInfo> DataList => _dataList;

	public TbModVegetationSpawnExtension(JSONNode _json)
	{
		_dataList = new List<ModVegetationSpawnExtensionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModVegetationSpawnExtensionInfo item = ModVegetationSpawnExtensionInfo.DeserializeModVegetationSpawnExtensionInfo(child);
			_dataList.Add(item);
		}
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModVegetationSpawnExtensionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModVegetationSpawnExtensionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
