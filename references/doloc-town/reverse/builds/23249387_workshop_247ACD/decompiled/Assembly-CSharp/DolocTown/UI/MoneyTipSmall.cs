using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MoneyTipSmall : MoneyTip
{
	[SerializeField]
	private Image coinIcon;

	private string prefix;

	public override void SetMoney(int dest, bool useAnimation = true)
	{
		base.SetMoney(dest, useAnimation);
		if (useAnimation && currentError != 0 && !(coinIcon == null))
		{
			if (currentError > 0)
			{
				coinIcon.RaiseUiSpriteFadeDown();
			}
			else
			{
				coinIcon.RaiseUiSpriteFadeUp();
			}
		}
	}
}
