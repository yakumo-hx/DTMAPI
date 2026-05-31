using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class UIEffectsAttention : UIEffects
{
	protected override Tween Start(RectTransform target)
	{
		return target.DOPunchScale(Vector3.one * 0.1f, 0.5f);
	}
}
