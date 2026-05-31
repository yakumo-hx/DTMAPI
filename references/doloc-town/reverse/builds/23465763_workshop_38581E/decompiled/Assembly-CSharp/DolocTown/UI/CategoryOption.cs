using System;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CategoryOption : DolocUiObject
{
	[SerializeField]
	private Text titleLabel;

	[SerializeField]
	private Text optionLabel;

	[SerializeField]
	private DolocButtonComponent prevBtn;

	[SerializeField]
	private DolocButtonComponent nextBtn;

	[SerializeField]
	private Image bgImage;

	private int _currentIndex = -1;

	private int _optionCount;

	private Func<int, string> _optionLabelGetter;

	private Func<int, int> _valueGetter;

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
			SetOptionLabel(num);
			OnValueChanged(_valueGetter(num));
			_currentIndex = num;
		}
	}

	public int CurrentValue => _valueGetter(CurrentIndex);

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

	public string OptionLabel => optionLabel.text;

	private int maxIndex => _optionCount - 1;

	protected override void __Init()
	{
		base.__Init();
		prevBtn.onClick.AddListener(OnPrevClick);
		nextBtn.onClick.AddListener(OnNextClick);
	}

	public void SetBgImageAlpha(int a)
	{
		Color color = bgImage.color;
		color.a = a;
		bgImage.color = color;
	}

	public void InitValue(int currentIndex, int optionCount, Func<int, string> optionLabelGetter, Func<int, int> valueGetter)
	{
		_currentIndex = currentIndex;
		_optionCount = optionCount;
		_optionLabelGetter = optionLabelGetter;
		_valueGetter = valueGetter;
		CurrentIndex = _currentIndex;
		prevBtn.gameObject.SetActive(optionCount > 1);
		nextBtn.gameObject.SetActive(optionCount > 1);
	}

	public void ClickNext()
	{
		nextBtn.FireClick();
	}

	public void ClickPrev()
	{
		prevBtn.FireClick();
	}

	private void OnPrevClick()
	{
		if (_optionCount > 1)
		{
			CurrentIndex = (CurrentIndex + _optionCount - 1) % _optionCount;
		}
	}

	private void OnNextClick()
	{
		if (_optionCount > 1)
		{
			CurrentIndex = (CurrentIndex + 1) % _optionCount;
		}
	}

	private void SetOptionLabel(int index)
	{
		optionLabel.text = _optionLabelGetter(index);
	}
}
