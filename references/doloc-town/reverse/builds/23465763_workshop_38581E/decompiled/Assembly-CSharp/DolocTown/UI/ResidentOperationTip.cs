using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class ResidentOperationTip : DolocUiRecyclableObject
{
	[SerializeField]
	private TMP_Text _text;

	[SerializeField]
	private float fadeDuration = 0.1f;

	private Vector2 positionWS;

	private bool isFollowingWorldPosition;

	private CanvasGroup canvasGroup;

	private Tween tween;

	public string text
	{
		get
		{
			return _text.text;
		}
		set
		{
			_text.text = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Update()
	{
		if (isFollowingWorldPosition)
		{
			base.transform.position = DolocAPI.WorldToScreen(positionWS);
		}
	}

	public void RaiseAsUI(Vector2 positionUI, string prompt)
	{
		_text.text = prompt;
		isFollowingWorldPosition = false;
		base.transform.position = positionUI;
		FadeIn();
	}

	public void RaiseAsFollowWS(Vector2 positionWS, string prompt)
	{
		_text.text = prompt;
		this.positionWS = positionWS;
		isFollowingWorldPosition = true;
		base.transform.position = DolocAPI.WorldToScreen(positionWS);
		FadeIn();
	}

	private void FadeIn()
	{
		tween?.Kill();
		canvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: true);
		tween = canvasGroup.DOFade(1f, fadeDuration);
	}
}
