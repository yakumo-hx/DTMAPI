using Newtonsoft.Json;
using UnityEngine;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public class Counter
{
	[JsonProperty]
	private int value;

	[JsonProperty]
	private int interval;

	private float intervalReciprotal;

	public int Interval => interval;

	public float Process => (float)value * intervalReciprotal;

	public int Value => value;

	public bool EqualToZero => value == 0;

	public Counter(int interval)
	{
		this.interval = interval;
		intervalReciprotal = 1f / (float)interval;
	}

	public Counter()
	{
		interval = 1;
		intervalReciprotal = 1f;
	}

	[JsonConstructor]
	public Counter(int value, int interval)
	{
		this.value = value;
		this.interval = interval;
		intervalReciprotal = 1f / (float)interval;
	}

	public void SetValue(int v)
	{
		value = Mathf.Clamp(v, 0, interval);
	}

	public void ValidateInterval(int newInterval)
	{
		if (newInterval > 0 && interval != newInterval)
		{
			interval = newInterval;
			intervalReciprotal = 1f / (float)interval;
			value = Mathf.Clamp(value, 0, interval);
		}
	}

	public void SetInterval(int interval, bool shouldReverse = false)
	{
		if (interval > 0)
		{
			this.interval = interval;
			intervalReciprotal = 1f / (float)interval;
			value = (shouldReverse ? interval : 0);
		}
	}

	public bool Tick()
	{
		if (++value >= interval)
		{
			value = 0;
			return true;
		}
		return false;
	}

	public bool DownTick()
	{
		if (--value <= 0)
		{
			value = interval;
			return true;
		}
		return false;
	}

	public bool DownTickNoLoop()
	{
		if (--value <= 0)
		{
			value = 0;
			return true;
		}
		return false;
	}

	public void Reset(int value = 0)
	{
		this.value = value;
	}

	public Counter Clone()
	{
		Counter counter = new Counter(interval);
		counter.SetValue(value);
		return counter;
	}

	public override string ToString()
	{
		return $"{value}/{interval}";
	}
}
