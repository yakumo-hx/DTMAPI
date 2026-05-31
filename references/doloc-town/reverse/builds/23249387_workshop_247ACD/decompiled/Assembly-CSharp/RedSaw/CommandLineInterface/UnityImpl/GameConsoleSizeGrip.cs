using UnityEngine;
using UnityEngine.UI;

namespace RedSaw.CommandLineInterface.UnityImpl;

[RequireComponent(typeof(Image))]
public class GameConsoleSizeGrip : GameConsoleClickable
{
	private RectTransform resizeTarget;

	private Vector2 startPos;

	private Vector2 resizeStartSize;

	private Vector2 resizeStartPos;

	private RectTransform sizeGripRect;

	public void Init(RectTransform target)
	{
		Init();
		resizeTarget = target;
		sizeGripRect = GetComponent<RectTransform>();
	}

	private void Update()
	{
		if (isDown)
		{
			Vector2 vector = base.MousePosition - startPos;
			resizeTarget.sizeDelta = resizeStartSize + new Vector2(vector.x, 0f - vector.y);
			sizeGripRect.localPosition = Vector2.zero;
		}
	}

	protected override void OnMouseButtonDown(Vector2 pos)
	{
		startPos = pos;
		resizeStartSize = resizeTarget.sizeDelta;
		resizeStartPos = base.transform.position;
	}
}
