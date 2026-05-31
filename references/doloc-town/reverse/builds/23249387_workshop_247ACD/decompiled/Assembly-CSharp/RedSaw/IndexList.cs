using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public class IndexList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable where T : class, IHasIndex
{
	[JsonProperty]
	private readonly IndexCounter counter = new IndexCounter();

	[JsonProperty]
	private readonly List<T> list = new List<T>();

	public T this[int index]
	{
		get
		{
			return list[index];
		}
		set
		{
			throw new Exception("IndexArray can't be modified by index");
		}
	}

	public int Count => list.Count;

	public bool IsReadOnly => false;

	public IndexList()
	{
	}

	[JsonConstructor]
	protected IndexList(IndexCounter counter, List<T> list)
	{
		this.counter = counter;
		foreach (T item in list)
		{
			if (item.isDeserializationValid)
			{
				this.list.Add(item);
			}
			else
			{
				Debug.LogError($"<{item.GetType().Name}>: 数据{item.index}无效，已移除!");
			}
		}
	}

	public bool IsRecycled(int index)
	{
		return counter.IsRecycled(index);
	}

	public T Sample()
	{
		if (list.Count <= 0)
		{
			return null;
		}
		return list[0];
	}

	public T RandomSample()
	{
		if (list.Count <= 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public T GetDataOrDefault(int index)
	{
		return list.FirstOrDefault((T data) => data.index == index);
	}

	public void Add(T item)
	{
		if (item != null && !list.Contains(item))
		{
			item.index = counter.NextIndex;
			list.Add(item);
		}
	}

	public bool Remove(T item)
	{
		if (item == null || !list.Remove(item))
		{
			return false;
		}
		counter.Remove(item.index);
		item.index = -1;
		return true;
	}

	public void RemoveAt(int index)
	{
		Remove(list[index]);
	}

	public void Clear()
	{
		counter.Clear();
		list.Clear();
	}

	public bool Contains(T item)
	{
		return list.Contains(item);
	}

	public int IndexOf(T item)
	{
		if (item == null)
		{
			return -1;
		}
		if (list.Contains(item))
		{
			return item.index;
		}
		return -1;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return list.GetEnumerator();
	}

	public void Insert(int index, T item)
	{
		throw new Exception("IndexArray can't be modified by index");
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		list.CopyTo(array, arrayIndex);
	}
}
