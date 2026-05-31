using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class GunModeMenu : DolocUiEntity
{
	private void Test()
	{
		GetComponent<RectTransform>().DOPunchScale(Vector3.one * 0.1f, 0.5f);
	}
}
