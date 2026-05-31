using UnityEngine;

namespace DolocTown.UI;

public class QuantitySubmitData : IUIData
{
	public string title;

	public string info;

	public int maxCount;

	public int initCount;

	public bool notEmpty { get; protected set; }

	public QuantitySubmitData(int maxCount, int initCount)
	{
		notEmpty = maxCount > 0;
		this.maxCount = Mathf.Max(1, maxCount);
		this.initCount = Mathf.Clamp(initCount, 1, maxCount);
	}
}
