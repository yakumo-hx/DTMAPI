using UnityEngine;
using UnityEngine.UI;

namespace RedSaw.CommandLineInterface.UnityImpl;

[RequireComponent(typeof(Image))]
public class GameConsoleHeader : GameConsoleClickable
{
	private RectTransform movTarget;

	private Vector2 movPos;

	private Vector2 startPos;

	public Color HeaderBarColor
	{
		get
		{
			return image.color;
		}
		set
		{
			image.color = value;
			normalColor = value;
		}
	}

	public void Init(RectTransform target)
	{
		Init();
		movTarget = target;
	}

	private void Update()
	{
		if (isDown)
		{
			movTarget.position = movPos + base.MousePosition - startPos;
		}
	}

	protected override void OnMouseButtonDown(Vector2 pos)
	{
		startPos = pos;
		movPos = movTarget.position;
	}
}
