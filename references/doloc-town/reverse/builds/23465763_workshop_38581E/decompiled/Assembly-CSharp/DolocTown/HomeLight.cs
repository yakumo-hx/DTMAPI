using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

[RequireComponent(typeof(Light2D))]
public class HomeLight : MonoBehaviour
{
	[SerializeField]
	private Color[] colors;

	private void SetColor(Color color)
	{
		Light2D component = GetComponent<Light2D>();
		if (!(component == null))
		{
			component.color = color;
		}
	}

	public void ChangeColor(int index)
	{
		if (!colors.IsNullOrEmpty() && index >= 0 && index < colors.Length)
		{
			SetColor(colors[index]);
		}
	}

	public void ChangeColorAccordingToTime()
	{
		if (!colors.IsNullOrEmpty())
		{
			if (colors.Length == 1)
			{
				SetColor(colors[0]);
				return;
			}
			int hour = DateTime.Now.Hour;
			int num = colors.Length;
			int num2 = (int)((float)hour / 24f * (float)num);
			SetColor(colors[num2]);
		}
	}
}
