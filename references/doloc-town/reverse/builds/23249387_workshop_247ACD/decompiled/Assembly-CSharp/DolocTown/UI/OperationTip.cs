using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public class OperationTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Ease ease = Ease.OutExpo;

	[SerializeField]
	private float showTime = 0.2f;

	[SerializeField]
	private float minPromptWidth = 240f;

	[SerializeField]
	private float pushOffset = 25f;

	[SerializeField]
	private Text textKey;

	[SerializeField]
	private Text textPrompt;

	private float paddingSize;

	private bool updateWorldPosition;

	private Vector2 wp;

	private DolocTweenScale anim;

	private DolocTweenMove pushAnim;

	private float showingHeight;

	private bool isPushing;

	public float Width => base.size.x;

	public bool isActive
	{
		get
		{
			if (base.gameObject.activeSelf)
			{
				return base.transform.localScale.Equals(Vector3.one);
			}
			return false;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		anim = new DolocTweenScale(base.transform, ease, showTime);
		pushAnim = new DolocTweenMove(base.transform, Ease.OutBack, showTime);
	}

	public void SetWp(Vector2 wp)
	{
		this.wp = wp;
	}

	public void UpdatePos()
	{
		updateWorldPosition = true;
		base.position = DolocAPI.WorldToScreen(wp);
		showingHeight = base.positionY;
	}

	public void SetScreenPosition(Vector2 pos)
	{
		updateWorldPosition = false;
		base.position = pos;
		showingHeight = pos.y;
	}

	public void Render(string key, string content)
	{
		updateWorldPosition = false;
		textKey.text = key;
		textPrompt.text = content;
		textPrompt.rectTransform.sizeDelta = new Vector2(Mathf.Max(textPrompt.preferredWidth, minPromptWidth), textPrompt.rectTransform.sizeDelta.y);
	}

	private void LateUpdate()
	{
		if (updateWorldPosition)
		{
			Vector2 vector = DolocAPI.WorldToScreen(wp);
			if (isPushing)
			{
				base.positionX = vector.x;
				return;
			}
			base.position = vector;
			showingHeight = vector.y;
		}
	}

	public void Show()
	{
		SetVisible(value: true);
		base.transform.localScale = Vector3.up;
		anim.forcePlay(Vector3.one);
	}

	public void Hide()
	{
		if (isPushing)
		{
			isPushing = false;
			pushAnim.stop();
		}
		updateWorldPosition = false;
		anim.forcePlay(Vector3.up, delegate
		{
			SetVisible(value: false);
		});
	}

	public void HideNotInvisible()
	{
		if (isPushing)
		{
			isPushing = false;
			pushAnim.stop();
		}
		anim.forcePlay(new Vector3(0f, 1f, 1f));
	}

	public void Push()
	{
		if (!isPushing)
		{
			isPushing = true;
			Vector3 currentPosition = base.position;
			pushAnim.setParams(Ease.OutExpo, 0.1f);
			pushAnim.forcePlay(new Vector3(currentPosition.x, currentPosition.y - pushOffset), delegate
			{
				pushAnim.setParams(Ease.OutBack, 0.1f);
				pushAnim.forcePlay(new Vector3(currentPosition.x, currentPosition.y));
				isPushing = false;
			});
		}
	}

	public void Push(Action action)
	{
		if (isPushing)
		{
			return;
		}
		isPushing = true;
		Vector3 currentPosition = base.position;
		pushAnim.setParams(Ease.OutExpo, 0.1f);
		pushAnim.forcePlay(new Vector3(currentPosition.x, currentPosition.y - pushOffset), delegate
		{
			pushAnim.setParams(Ease.OutBack, 0.1f);
			pushAnim.forcePlay(currentPosition, delegate
			{
				action?.Invoke();
				isPushing = false;
			});
		});
	}
}
