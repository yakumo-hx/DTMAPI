using System;
using System.Collections.Generic;

namespace DolocTown.GameData;

public class EnumDatabase<TKey, TValue> where TKey : Enum where TValue : class
{
	private TValue[] datas;

	private Dictionary<string, TValue> datasEx = new Dictionary<string, TValue>();

	private TValue NULL;

	private int usefulStdCount;

	private TValue defaultValue { get; set; }

	public int totalCount => usefulStdCount + datasEx.Count;

	public EnumDatabase()
	{
		datas = new TValue[Enum.GetNames(typeof(TKey)).Length];
		datasEx = new Dictionary<string, TValue>();
		usefulStdCount = 0;
		defaultValue = null;
		NULL = defaultValue;
	}

	public bool AddData(TKey key, TValue data)
	{
		int hashCode = key.GetHashCode();
		if (datas[hashCode] == NULL)
		{
			datas[hashCode] = data;
			usefulStdCount++;
			return true;
		}
		return false;
	}

	public void AddExtraData(string name, TValue data)
	{
		if (datasEx.ContainsKey(name))
		{
			datasEx[name] = data;
		}
		else
		{
			datasEx.Add(name, data);
		}
	}

	public bool QueryExtraData(string name, out TValue data)
	{
		return datasEx.TryGetValue(name, out data);
	}

	public TValue GetData(TKey key)
	{
		return datas[key.GetHashCode()];
	}
}
