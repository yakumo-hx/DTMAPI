using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbMainMenu
{
	private readonly Dictionary<MenuType, MainMenuInfo> _dataMap;

	private readonly List<MainMenuInfo> _dataList;

	public Dictionary<MenuType, MainMenuInfo> DataMap => _dataMap;

	public List<MainMenuInfo> DataList => _dataList;

	public MainMenuInfo this[MenuType key] => _dataMap[key];

	public TbMainMenu(JSONNode _json)
	{
		_dataMap = new Dictionary<MenuType, MainMenuInfo>();
		_dataList = new List<MainMenuInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MainMenuInfo mainMenuInfo = MainMenuInfo.DeserializeMainMenuInfo(child);
			if (_dataMap.TryAdd(mainMenuInfo.Id, mainMenuInfo))
			{
				_dataList.Add(mainMenuInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {mainMenuInfo.Id} in table: TbMainMenu");
			}
		}
	}

	public MainMenuInfo GetOrDefault(MenuType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MainMenuInfo Get(MenuType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MainMenuInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MainMenuInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
