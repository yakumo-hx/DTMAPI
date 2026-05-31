using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbItemOrder
{
	private readonly Dictionary<string, ItemOrderInfo> _dataMap;

	private readonly List<ItemOrderInfo> _dataList;

	public Dictionary<string, ItemOrderInfo> DataMap => _dataMap;

	public List<ItemOrderInfo> DataList => _dataList;

	public ItemOrderInfo this[string key] => _dataMap[key];

	public TbItemOrder(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ItemOrderInfo>();
		_dataList = new List<ItemOrderInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ItemOrderInfo itemOrderInfo = ItemOrderInfo.DeserializeItemOrderInfo(child);
			if (_dataMap.TryAdd(itemOrderInfo.Id, itemOrderInfo))
			{
				_dataList.Add(itemOrderInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + itemOrderInfo.Id + " in table: TbItemOrder");
			}
		}
	}

	public ItemOrderInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ItemOrderInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemOrderInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemOrderInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		for (int i = 0; i < _dataList.Count; i++)
		{
			_dataList[i].SortingOrder = i + 1;
		}
	}
}
