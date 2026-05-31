using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BubbleDialogPointer : DolocUiObject
{
	[SerializeField]
	private Transform icon;

	[SerializeField]
	private Shadow shadow;

	public void SetPosition(float offsetX, float parentWidth)
	{
		int num = ((!(offsetX < 0f)) ? 1 : (-1));
		icon.localScale = new Vector3(num, 1f, 1f);
		shadow.effectDistance = new Vector2Int(4 * num, -4);
		Vector3 zero = Vector3.zero;
		zero.x = Mathf.Clamp(0f - offsetX, -0.5f * parentWidth + 24f, 0.5f * parentWidth - 24f);
		icon.localPosition = zero;
	}
}
