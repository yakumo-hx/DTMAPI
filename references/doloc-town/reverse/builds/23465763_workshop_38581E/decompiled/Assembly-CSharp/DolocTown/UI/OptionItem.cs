using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class OptionItem : DolocUiObject
{
	[SerializeField]
	private Text titleLabel;

	[SerializeField]
	private Text optionLabel;

	[SerializeField]
	private DolocSelectableComponent textBox;

	[SerializeField]
	private DolocButtonComponent prevBtn;

	[SerializeField]
	private DolocButtonComponent nextBtn;

	[SerializeField]
	private Image bgImage;

	private int _currentIndex = -1;

	private int optionCount;

	private Func<int, string> OptionLabelGetter;

	private Func<int, int> ValueGetter;

	private Action selectCallback;

	public Action<int> OnValueChanged { get; set; }

	public int CurrentIndex
	{
		get
		{
			return _currentIndex;
		}
		set
		{
			int num = Mathf.Clamp(value, 0, maxIndex);
			OnValueChanged(ValueGetter(num));
			SetOptionLabel(num);
			_currentIndex = num;
		}
	}

	public int CurrentValue => ValueGetter(CurrentIndex);

	public string Title
	{
		get
		{
			return titleLabel.text;
		}
		set
		{
			titleLabel.text = value;
		}
	}

	private int maxIndex => optionCount - 1;

	public Selectable Selectable => textBox;

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
		textBox.onSelect.AddListener(delegate
		{
			SetBgImageAlpha(1);
			selectCallback?.Invoke();
		});
		textBox.onDeselect.AddListener(delegate
		{
			SetBgImageAlpha(0);
		});
	}

	private void OnPrevClick()
	{
		CurrentIndex = (CurrentIndex + optionCount - 1) % optionCount;
		textBox.Select();
	}

	private void OnNextClick()
	{
		CurrentIndex = (CurrentIndex + 1) % optionCount;
		textBox.Select();
	}

	private void SetOptionLabel(int index)
	{
		optionLabel.text = OptionLabelGetter(index);
	}

	public void SetBgImageAlpha(int a)
	{
		Color color = bgImage.color;
		color.a = a;
		bgImage.color = color;
	}

	public void InitValue(int currentIndex, int optionCount, Func<int, string> optionLabelGetter, Func<int, int> ValueGetter)
	{
		_currentIndex = currentIndex;
		this.optionCount = optionCount;
		OptionLabelGetter = optionLabelGetter;
		this.ValueGetter = ValueGetter;
		CurrentIndex = _currentIndex;
	}

	public void SetSelectCallback(Action callback)
	{
		selectCallback = callback;
	}
}
