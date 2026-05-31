using UnityEngine;
using UnityEngine.Events;

namespace DolocTown.UI;

public class QuantitySubmitBar : DolocUiObject
{
	[SerializeField]
	private AutoSizeText countText;

	[SerializeField]
	private AutoSizeText minText;

	[SerializeField]
	private AutoSizeText maxText;

	[SerializeField]
	private DolocNavigationButton btnSubOne;

	[SerializeField]
	private DolocNavigationButton btnSubTen;

	[SerializeField]
	private DolocNavigationButton btnAddOne;

	[SerializeField]
	private DolocNavigationButton btnAddTen;

	[SerializeField]
	private DolocNavigationButton btnMin;

	[SerializeField]
	private DolocNavigationButton btnMax;

	private int minCount;

	private int maxCount;

	private int _currentCount;

	public UnityEvent<int> onCurrentCountChange = new UnityEvent<int>();

	public int currentCount
	{
		get
		{
			return _currentCount;
		}
		private set
		{
			_currentCount = value;
			OnCurrentCountChange(value);
			onCurrentCountChange.Invoke(value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		countText.Init();
		minText.Init();
		maxText.Init();
		InitCountButton(btnSubOne, -1);
		InitCountButton(btnSubTen, -10);
		InitCountButton(btnAddOne, 1);
		InitCountButton(btnAddTen, 10);
		btnMin.Init();
		btnMin.SetClickCallbacks(delegate
		{
			SetMin();
		});
		btnMax.Init();
		btnMax.onClick.AddListener(delegate
		{
			SetMax();
		});
	}

	private void InitCountButton(DolocNavigationButton button, int diff)
	{
		button.Init();
		button.SetClickCallbacks(delegate
		{
			AddDiff(diff);
		}, null, null, null, null, null, (int _) => AddDiff(diff));
	}

	public bool AddDiff(int diff)
	{
		if (currentCount == minCount && diff < 0)
		{
			SetMax();
			return true;
		}
		if (currentCount == maxCount && diff > 0)
		{
			SetMin();
			return true;
		}
		int num = currentCount + diff;
		currentCount = Mathf.Clamp(num, minCount, maxCount);
		if ((diff <= 0 || currentCount != maxCount) && (diff >= 0 || currentCount != minCount))
		{
			return num == currentCount;
		}
		return false;
	}

	public void SetMin()
	{
		currentCount = minCount;
	}

	public void SetMax()
	{
		currentCount = maxCount;
	}

	private void OnCurrentCountChange(int value)
	{
		countText.text = value.ToString();
	}

	public void Render(QuantitySubmitData data)
	{
		minCount = 1;
		minText.text = minCount.ToString();
		maxCount = data.maxCount;
		maxText.text = maxCount.ToString();
		currentCount = data.initCount;
	}
}
