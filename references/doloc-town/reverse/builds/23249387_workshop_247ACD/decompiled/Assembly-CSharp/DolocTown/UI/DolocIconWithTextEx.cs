using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class DolocIconWithTextEx : DolocIcon
{
	[SerializeField]
	private TextMeshProUGUI textMesh;

	[SerializeField]
	private CanvasGroup canvasGroup;

	public int number
	{
		set
		{
			textMesh.text = value.ToString();
		}
	}

	public string description
	{
		set
		{
			textMesh.text = value ?? "";
		}
	}

	public override float alpha
	{
		set
		{
			canvasGroup.alpha = value;
		}
	}

	public bool interactable
	{
		set
		{
			canvasGroup.interactable = value;
		}
	}
}
