using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class OptionItemUI : SettingValueItemUI<string>
{
	[SerializeField]
	public Text optionLabel;

	[SerializeField]
	public DolocSelectableComponent textBox;

	[SerializeField]
	public DolocButtonComponent prevBtn;

	[SerializeField]
	public DolocButtonComponent nextBtn;

	[SerializeField]
	public CanvasGroup canvasGroup;

	private int _currentIndex = -1;

	private int optionCount;

	private Func<int, string> OptionLabelGetter;

	private Func<int, string> ValueGetter;

	public int currentIndex
	{
		get
		{
			return _currentIndex;
		}
		set
		{
			int num = Mathf.Clamp(value, 0, maxIndex);
			if (_currentIndex != num)
			{
				onValueChanged.Invoke(GetValue(num));
				SetOptionLabel(num);
				_currentIndex = num;
			}
		}
	}

	public override string currentValue => GetValue(currentIndex);

	private int maxIndex => optionCount - 1;

	public override Selectable selectable => textBox;

	protected override void __Init()
	{
		base.__Init();
		prevBtn.onClick.AddListener(OnPrevClick);
		nextBtn.onClick.AddListener(OnNextClick);
		textBox.onMove.AddListener(delegate(MoveDirection dir)
		{
			switch (dir)
			{
			case MoveDirection.Left:
				prevBtn.FireClick();
				break;
			case MoveDirection.Right:
				nextBtn.FireClick();
				break;
			}
		});
	}

	private void OnPrevClick()
	{
		currentIndex = (currentIndex + optionCount - 1) % optionCount;
		textBox.Select();
	}

	private void OnNextClick()
	{
		currentIndex = (currentIndex + 1) % optionCount;
		textBox.Select();
	}

	private string GetValue(int index)
	{
		try
		{
			return ValueGetter(index);
		}
		catch (Exception message)
		{
			Debug.LogError($"<{base.settingId}>获取选项值失败，index: {index}");
			Debug.LogError(message);
			return null;
		}
	}

	private void SetOptionLabel(int index)
	{
		try
		{
			optionLabel.text = OptionLabelGetter(index);
		}
		catch (Exception message)
		{
			Debug.LogError($"<{base.settingId}>获取选项值失败，index: {index}");
			Debug.LogError(message);
			optionLabel.text = string.Empty;
			throw;
		}
	}

	public void InitValue(int currentIndex, int optionCount, Func<int, string> OptionLabelGetter, Func<int, string> ValueGetter)
	{
		_currentIndex = currentIndex;
		this.optionCount = optionCount;
		this.OptionLabelGetter = OptionLabelGetter;
		this.ValueGetter = ValueGetter;
		SetOptionLabel(currentIndex);
	}

	public void SetGrayed(bool value)
	{
		canvasGroup.alpha = (value ? 0.3f : 1f);
	}
}
