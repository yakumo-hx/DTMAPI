using System;
using System.Collections.Generic;

namespace XLua;

public class ObjectPool
{
	private struct Slot
	{
		public int next;

		public object obj;

		public Slot(int next, object obj)
		{
			this.next = next;
			this.obj = obj;
		}
	}

	private const int LIST_END = -1;

	private const int ALLOCED = -2;

	private Slot[] list = new Slot[512];

	private int freelist = -1;

	private int count;

	public object this[int i]
	{
		get
		{
			if (i >= 0 && i < count)
			{
				return list[i].obj;
			}
			return null;
		}
	}

	public void Clear()
	{
		freelist = -1;
		count = 0;
		list = new Slot[512];
	}

	private void extend_capacity()
	{
		Slot[] array = new Slot[list.Length * 2];
		for (int i = 0; i < list.Length; i++)
		{
			array[i] = list[i];
		}
		list = array;
	}

	public int Add(object obj)
	{
		int num = -1;
		if (freelist != -1)
		{
			num = freelist;
			list[num].obj = obj;
			freelist = list[num].next;
			list[num].next = -2;
		}
		else
		{
			if (count == list.Length)
			{
				extend_capacity();
			}
			num = count;
			list[num] = new Slot(-2, obj);
			count = num + 1;
		}
		return num;
	}

	public bool TryGetValue(int index, out object obj)
	{
		if (index >= 0 && index < count && list[index].next == -2)
		{
			obj = list[index].obj;
			return true;
		}
		obj = null;
		return false;
	}

	public object Get(int index)
	{
		if (index >= 0 && index < count)
		{
			return list[index].obj;
		}
		return null;
	}

	public object Remove(int index)
	{
		if (index >= 0 && index < count && list[index].next == -2)
		{
			object obj = list[index].obj;
			list[index].obj = null;
			list[index].next = freelist;
			freelist = index;
			return obj;
		}
		return null;
	}

	public object Replace(int index, object o)
	{
		if (index >= 0 && index < count)
		{
			object obj = list[index].obj;
			list[index].obj = o;
			return obj;
		}
		return null;
	}

	public int Check(int check_pos, int max_check, Func<object, bool> checker, Dictionary<object, int> reverse_map)
	{
		if (count == 0)
		{
			return 0;
		}
		for (int i = 0; i < Math.Min(max_check, count); i++)
		{
			check_pos %= count;
			if (list[check_pos].next == -2 && list[check_pos].obj != null && !checker(list[check_pos].obj))
			{
				object key = Replace(check_pos, null);
				if (reverse_map.TryGetValue(key, out var value) && value == check_pos)
				{
					reverse_map.Remove(key);
				}
			}
			check_pos++;
		}
		return check_pos %= count;
	}
}
