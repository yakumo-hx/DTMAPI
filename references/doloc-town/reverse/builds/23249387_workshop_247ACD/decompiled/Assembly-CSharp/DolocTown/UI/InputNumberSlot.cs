using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class InputNumberSlot : DolocNavigationButton
{
	[SerializeField]
	private CanvasGroup arrowCanvasGroup;

	[SerializeField]
	private DolocNavigationButton btnUp;

	[SerializeField]
	private DolocNavigationButton btnDown;

	[SerializeField]
	private Text numberText;

	[SerializeField]
	private int padWidth = 2;

	private int minValue;

	private int maxValue;

	private int _currentValue;

	public UnityEvent<int> onCurrentValueChange = new UnityEvent<int>();

	public int currentValue
	{
		get
		{
			return _currentValue;
		}
		private set
		{
			_currentValue = value;
			OnCurrentValueChange(value);
			onCurrentValueChange.Invoke(value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		OnHighLighted(value: false);
		InitValueButton(btnUp, 1);
		InitValueButton(btnDown, -1);
	}

	private void InitValueButton(DolocNavigationButton button, int diff)
	{
		button.Init();
		button.SetClickCallbacks(delegate
		{
			AddDiff(diff);
		}, null, null, null, null, null, (int _) => AddDiff(diff));
	}

	public bool AddDiff(int diff)
	{
		if (currentValue == minValue && diff < 0)
		{
			SetMax();
			return true;
		}
		if (currentValue == maxValue && diff > 0)
		{
			SetMin();
			return true;
		}
		int num = currentValue + diff;
		currentValue = Mathf.Clamp(num, minValue, maxValue);
		if ((diff <= 0 || currentValue != maxValue) && (diff >= 0 || currentValue != minValue))
		{
			return num == currentValue;
		}
		return false;
	}

	public void SetMin()
	{
		currentValue = minValue;
	}

	public void SetMax()
	{
		currentValue = maxValue;
	}

	private void OnCurrentValueChange(int value)
	{
		numberText.text = value.ToString($"D{padWidth}");
	}

	public void Render(int minValue, int maxValue, int initValue)
	{
		this.minValue = minValue;
		this.maxValue = maxValue;
		currentValue = Mathf.Clamp(initValue, minValue, maxValue);
	}

	protected override void OnHighLighted(bool value)
	{
		arrowCanvasGroup.alpha = (value ? 1f : 0f);
	}
}
