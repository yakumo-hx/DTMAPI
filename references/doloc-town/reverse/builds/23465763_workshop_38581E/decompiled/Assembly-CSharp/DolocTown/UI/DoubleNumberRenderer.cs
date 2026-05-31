using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class DoubleNumberRenderer : DolocUiObject
{
	[SerializeField]
	private NumberRenderer number1;

	[SerializeField]
	private NumberRenderer number2;

	[SerializeField]
	private Sprite[] numberSprites;

	private bool invalid;

	private bool IsNumberSpritesValid()
	{
		if (numberSprites != null)
		{
			return numberSprites.Length == 10;
		}
		return false;
	}

	protected override void __Init()
	{
		base.__Init();
		invalid = !IsNumberSpritesValid();
	}

	public void SetNumber(int level)
	{
		if (!invalid)
		{
			level = Mathf.Min(level, 99);
			number1.sprite = numberSprites[level / 10];
			number2.sprite = numberSprites[level % 10];
		}
	}

	public void TweenNumber(int level, float duration = 0.25f, Ease ease = Ease.OutBounce)
	{
		if (!invalid)
		{
			level = Mathf.Min(level, 99);
			int num = level / 10;
			int num2 = level % 10;
			number1.SetNumber(numberSprites[num], duration, ease);
			number2.SetNumber(numberSprites[num2], duration, ease);
		}
	}
}
