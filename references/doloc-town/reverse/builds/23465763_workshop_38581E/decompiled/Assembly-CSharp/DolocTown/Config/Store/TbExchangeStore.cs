using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Store;

public sealed class TbExchangeStore
{
	private readonly Dictionary<string, ExchangeStoreInfo> _dataMap;

	private readonly List<ExchangeStoreInfo> _dataList;

	public Dictionary<string, ExchangeStoreInfo> DataMap => _dataMap;

	public List<ExchangeStoreInfo> DataList => _dataList;

	public ExchangeStoreInfo this[string key] => _dataMap[key];

	public TbExchangeStore(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ExchangeStoreInfo>();
		_dataList = new List<ExchangeStoreInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ExchangeStoreInfo exchangeStoreInfo = ExchangeStoreInfo.DeserializeExchangeStoreInfo(child);
			if (_dataMap.TryAdd(exchangeStoreInfo.Id, exchangeStoreInfo))
			{
				_dataList.Add(exchangeStoreInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + exchangeStoreInfo.Id + " in table: TbExchangeStore");
			}
		}
	}

	public ExchangeStoreInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ExchangeStoreInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ExchangeStoreInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ExchangeStoreInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
