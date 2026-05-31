using System;
using System.Collections;
using System.Collections.Generic;

public class LRUCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
	private readonly int capacity;

	private readonly LinkedList<TKey> lruList = new LinkedList<TKey>();

	private readonly Dictionary<TKey, TValue> cache = new Dictionary<TKey, TValue>();

	private readonly HashSet<TKey> permanentKeys = new HashSet<TKey>();

	private readonly Action<TValue> onRemove;

	public TValue this[TKey key]
	{
		get
		{
			TryGetValue(key, out var value);
			return value;
		}
	}

	public IEnumerable<TKey> Keys => lruList;

	public IEnumerable<TValue> Values
	{
		get
		{
			foreach (TKey lru in lruList)
			{
				yield return cache[lru];
			}
		}
	}

	public LRUCache(int capacity, Action<TValue> onRemove = null)
	{
		this.capacity = capacity;
		this.onRemove = onRemove;
	}

	public bool ContainsKey(TKey key)
	{
		return cache.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		if (cache.TryGetValue(key, out value))
		{
			lruList.Remove(key);
			lruList.AddFirst(key);
			return true;
		}
		return false;
	}

	public bool Add(TKey key, TValue value, bool permanent = false)
	{
		if (cache.ContainsKey(key))
		{
			return false;
		}
		cache[key] = value;
		if (permanent)
		{
			permanentKeys.Add(key);
			return true;
		}
		lruList.AddFirst(key);
		if (lruList.Count <= capacity)
		{
			return true;
		}
		TKey value2 = lruList.Last.Value;
		TValue obj = cache[value2];
		lruList.RemoveLast();
		cache.Remove(value2);
		onRemove?.Invoke(obj);
		return true;
	}

	public bool Remove(TKey key)
	{
		if (!cache.ContainsKey(key))
		{
			return false;
		}
		TValue obj = cache[key];
		int result = 1 & (lruList.Remove(key) ? 1 : 0) & (cache.Remove(key) ? 1 : 0);
		permanentKeys.Remove(key);
		Action<TValue> action = onRemove;
		if (action != null)
		{
			action(obj);
			return (byte)result != 0;
		}
		return (byte)result != 0;
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		foreach (TKey lru in lruList)
		{
			yield return new KeyValuePair<TKey, TValue>(lru, cache[lru]);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
