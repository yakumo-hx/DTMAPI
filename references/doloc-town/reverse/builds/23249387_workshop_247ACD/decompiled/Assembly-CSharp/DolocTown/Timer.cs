using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Settings;
using UnityEngine;

namespace DolocTown;

public class Timer
{
	private Dictionary<int, float> intervalCache = new Dictionary<int, float>();

	private int lastNonConstantCount;

	private float lastInterval;

	private int currentRepeatCount;

	private float currentInterval;

	private float currentTime;

	public Timer(TimerInfo timerProto)
	{
		InitTimer(timerProto);
	}

	private void InitTimer(TimerInfo timerProto)
	{
		if (timerProto == null)
		{
			Debug.LogError("初始化定时器失败");
			return;
		}
		int num = 0;
		TimerTickInfo[] tickInfo = timerProto.TickInfo;
		foreach (TimerTickInfo timerTickInfo in tickInfo)
		{
			for (int j = 0; j < timerTickInfo.RepeatTime; j++)
			{
				intervalCache.Add(num++, timerTickInfo.Interval);
			}
		}
		lastNonConstantCount = num;
		lastInterval = timerProto.TickInfo.Last().Interval;
	}

	public void ReStart()
	{
		Clear();
	}

	public bool Update(float deltaTime)
	{
		currentTime += deltaTime;
		if (currentTime >= currentInterval)
		{
			currentTime = 0f;
			currentRepeatCount++;
			currentInterval = GetInterval(currentRepeatCount);
			return true;
		}
		return false;
	}

	private void Clear()
	{
		currentRepeatCount = 0;
		currentTime = 0f;
		currentInterval = GetInterval(0);
	}

	private float GetInterval(int repeatCount)
	{
		if (repeatCount < lastNonConstantCount)
		{
			return intervalCache[repeatCount];
		}
		return lastInterval;
	}
}
