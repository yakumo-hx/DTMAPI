using System;
using System.Collections.Generic;
using System.Text;
using RedSaw.CommandLineInterface;

namespace DolocTown.GameData;

[DebugObject]
public class SimpleDatabase<T>
{
	private readonly Dictionary<string, T> datas;

	private readonly Dictionary<string, T> datasEx;

	private T defaultValue { get; set; }

	[DebugInfo("数据库总数")]
	public int totalCount => datas.Count + datasEx.Count;

	public IEnumerable<T> totalValues
	{
		get
		{
			foreach (T value in datas.Values)
			{
				yield return value;
			}
			foreach (T value2 in datasEx.Values)
			{
				yield return value2;
			}
		}
	}

	[DebugInfo("所有数据列表")]
	private string allDataInfos
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string key in datas.Keys)
			{
				stringBuilder.Append(key + "\n");
			}
			return stringBuilder.ToString();
		}
	}

	public SimpleDatabase()
	{
		datas = new Dictionary<string, T>();
		datasEx = new Dictionary<string, T>();
		defaultValue = default(T);
	}

	public void AddData(string name, T data)
	{
		if (datas.ContainsKey(name))
		{
			AddDataEx(name, data);
		}
		else
		{
			datas.Add(name, data);
		}
	}

	public void AddDataEx(string name, T data)
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

	public bool QueryData(string name, out T data)
	{
		if (string.IsNullOrEmpty(name))
		{
			data = default(T);
			return false;
		}
		if (!datas.TryGetValue(name, out data))
		{
			return datasEx.TryGetValue(name, out data);
		}
		return true;
	}

	public bool ContainsData(string name)
	{
		if (!datas.ContainsKey(name))
		{
			return datasEx.ContainsKey(name);
		}
		return true;
	}

	public T GetData(string name)
	{
		return datas.GetValueOrDefault(name);
	}

	public T[] LoadDatas(string[] names)
	{
		if (names.IsNullOrEmpty())
		{
			return Array.Empty<T>();
		}
		T[] array = new T[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			array[i] = GetData(names[i]);
		}
		return array;
	}
}
