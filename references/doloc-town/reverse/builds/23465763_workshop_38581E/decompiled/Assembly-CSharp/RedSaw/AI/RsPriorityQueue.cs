using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.AI;

public class RsPriorityQueue<T> : IPriorityQueue<T>
{
	public int currentPriority = -1;

	public Dictionary<int, Stack<T>> sets;

	public bool empty => currentPriority == -1;

	public bool notEmpty => currentPriority >= 0;

	public RsPriorityQueue()
	{
		sets = new Dictionary<int, Stack<T>>();
	}

	public void set(T value, int p)
	{
		if (sets.ContainsKey(p))
		{
			sets[p].Push(value);
			return;
		}
		Stack<T> stack = new Stack<T>();
		stack.Push(value);
		sets.Add(p, stack);
		comparePriority(p);
	}

	public T get()
	{
		Stack<T> stack = sets[currentPriority];
		T result = stack.Pop();
		if (stack.Count == 0)
		{
			sets.Remove(currentPriority);
			findTop();
		}
		return result;
	}

	private void findTop()
	{
		if (sets.Count > 0)
		{
			do
			{
				currentPriority++;
			}
			while (!sets.ContainsKey(currentPriority));
		}
		else
		{
			currentPriority = -1;
		}
	}

	private void comparePriority(int p)
	{
		if (currentPriority < 0)
		{
			currentPriority = p;
		}
		else
		{
			currentPriority = Mathf.Min(currentPriority, p);
		}
	}

	public void clear()
	{
		currentPriority = -1;
		sets.Clear();
	}
}
