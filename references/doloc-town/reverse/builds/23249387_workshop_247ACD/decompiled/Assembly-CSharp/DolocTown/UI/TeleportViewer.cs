using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TeleportViewer : DolocUiObject
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtDesc;

	[SerializeField]
	private CanvasGroup canvasGroup;

	protected override void __Init()
	{
		base.__Init();
		canvasGroup.alpha = 0f;
	}

	public void Show(Vector2 positionLocal, string title, string description)
	{
		base.transform.localPosition = positionLocal;
		txtTitle.text = title;
		txtDesc.text = description;
		canvasGroup.DOFade(1f, 0.1f);
	}

	public void Hide()
	{
		canvasGroup.DOFade(0f, 0.1f);
	}
}
