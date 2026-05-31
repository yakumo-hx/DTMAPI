using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class BuffTimer
{
	[JsonProperty]
	public int currentTick;

	[JsonProperty]
	public int totalTick;

	public float Progress
	{
		get
		{
			if (totalTick > 0)
			{
				return (float)currentTick / (float)totalTick;
			}
			return 1f;
		}
	}

	public bool Permanent => totalTick < 0;

	public BuffTimer(int totalTick)
		: this(totalTick, totalTick)
	{
	}

	[JsonConstructor]
	private BuffTimer(int totalTick, int currentTick)
	{
		this.totalTick = totalTick;
		this.currentTick = currentTick;
	}

	public void Validate(int totalTick)
	{
		this.totalTick = Mathf.Min(this.totalTick, totalTick);
		currentTick = Mathf.Min(currentTick, totalTick);
	}

	public void Refresh()
	{
		currentTick = totalTick;
	}

	public bool Update()
	{
		if (Permanent)
		{
			return false;
		}
		return --currentTick <= 0;
	}
}
