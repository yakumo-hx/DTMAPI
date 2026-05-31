using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DataManager<T> where T : class, IHasIndex
{
	[JsonProperty]
	private readonly IndexList<T> datas = new IndexList<T>();

	public IEnumerable<T> AllDatas => datas;

	public bool IsEmpty => datas.Count == 0;

	public bool IsNotEmpty => datas.Count > 0;

	public T First => datas.Sample();

	public T Choice => datas.RandomSample();

	public DataManager()
	{
	}

	[JsonConstructor]
	public DataManager(IndexList<T> datas)
	{
		this.datas = datas;
	}

	public void AddData(T data)
	{
		datas.Add(data);
	}

	public bool RemoveData(T data)
	{
		return datas.Remove(data);
	}

	public void Clear()
	{
		datas.Clear();
	}

	public bool Find(Func<T, bool> condition, out T result)
	{
		result = null;
		if (datas.Count == 0)
		{
			return false;
		}
		foreach (T data in datas)
		{
			if (condition(data))
			{
				result = data;
				return true;
			}
		}
		return false;
	}
}
