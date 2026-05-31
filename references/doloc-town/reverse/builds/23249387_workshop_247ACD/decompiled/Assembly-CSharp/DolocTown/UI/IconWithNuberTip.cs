using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class IconWithNuberTip : NumberTip
{
	[SerializeField]
	private Image icon;

	private Sequence seq;

	protected override void __Init()
	{
		base.__Init();
		SetVisible(value: false);
	}

	public void Render(Vector3 worldPosition, float value)
	{
		base.WorldPosition = worldPosition;
		base.Color = Color.white;
		base.Number = value;
		SetVisible(value: true);
	}

	public void RollNumber(Color color, float endValue)
	{
		if (base.isVisible)
		{
			base.Color = color;
			seq?.Kill();
			seq = DOTween.Sequence();
			seq.Append(DOTween.To(() => base.Number, delegate(float x)
			{
				base.Number = x;
			}, endValue, 0.5f).OnComplete(delegate
			{
				base.Color = Color.white;
			}));
		}
	}

	public void SetIconSprite(Sprite sprite)
	{
		SetSprite(icon, sprite, autoSize: true);
	}
}
