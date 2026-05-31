using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DolocCoroutineLoader
{
	private int total;

	private Action callback;

	private Queue<IEnumerator> coroutines;

	public DolocCoroutineLoader(Action callback)
	{
		this.callback = callback;
		coroutines = new Queue<IEnumerator>();
	}

	public void OnCoroutineDone()
	{
		total--;
		Debug.Log($"AB包加载完毕,剩余:{total}");
		if (total == 0 && callback != null)
		{
			callback();
			callback = null;
		}
	}

	public void Start(MonoBehaviour driver)
	{
		total = coroutines.Count;
		while (coroutines.Count > 0)
		{
			driver.StartCoroutine(coroutines.Dequeue());
		}
	}

	public void AddCoroutine(IEnumerator coroutine)
	{
		coroutines.Enqueue(coroutine);
	}
}
