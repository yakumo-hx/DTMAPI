using Newtonsoft.Json;
using UnityEngine;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public class RSTimer
{
	[JsonProperty]
	private float currentTime;

	[JsonProperty]
	private float interval;

	[JsonProperty]
	private float max;

	public static RSTimer SecondTimer => new RSTimer();

	public float currentInterval => interval;

	public float process => currentTime / interval;

	public RSTimer(float interval = 1f)
	{
		currentTime = 0f;
		this.interval = interval;
	}

	[JsonConstructor]
	public RSTimer(float interval, float currentTime, float max)
	{
		this.interval = interval;
		this.currentTime = currentTime;
		this.max = max;
	}

	public void Reset()
	{
		currentTime = 0f;
	}

	public void SetInterval(float interval)
	{
		if (!(interval <= 0f))
		{
			this.interval = interval;
			currentTime = 0f;
		}
	}

	public void Clamp(float maxInterval)
	{
		if (!(interval < 0f))
		{
			interval = Mathf.Clamp(interval, 0f, maxInterval);
			currentTime = Mathf.Min(currentTime, interval);
		}
	}

	public void SetDownTick(float downtickMax)
	{
		max = downtickMax;
		currentTime = downtickMax;
	}

	public bool Tick(float deltaTime)
	{
		currentTime += deltaTime;
		if (currentTime >= interval)
		{
			currentTime -= interval;
			return true;
		}
		return false;
	}

	public float TickContinues(float deltaTime, bool autoReset = true)
	{
		currentTime -= deltaTime;
		float result = currentTime / max;
		if (currentTime <= 0f && autoReset)
		{
			currentTime = max;
		}
		return result;
	}

	public bool Offset(float value)
	{
		currentTime += value;
		return currentTime >= interval;
	}

	public void SetRandomInterval(float min, float max)
	{
		if (max < min)
		{
			SetInterval(min);
		}
		SetInterval(Random.Range(min, max));
	}
}
