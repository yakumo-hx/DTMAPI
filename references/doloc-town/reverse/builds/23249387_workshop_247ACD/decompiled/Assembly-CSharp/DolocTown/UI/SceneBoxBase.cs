using System;
using DG.Tweening;
using DolocTown.Config;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DolocTown.UI;

public class SceneBoxBase : DolocUiObject, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private DolocButtonComponent closeButton;

	private Vector2 worldPosition;

	private Tween tween;

	public bool isHover { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		closeButton.gameObject.SetActive(value: false);
		closeButton.onClick.AddListener(delegate
		{
			SetVisible(value: false);
		});
		Tables.LanguageChange = (Action)Delegate.Combine(Tables.LanguageChange, new Action(Refresh));
	}

	private void OnDestroy()
	{
		Tables.LanguageChange = (Action)Delegate.Remove(Tables.LanguageChange, new Action(Refresh));
	}

	public override void SetVisible(bool value)
	{
		closeButton.gameObject.SetActive(value: false);
		base.SetVisible(value);
	}

	private void Refresh()
	{
		SetVisible(value: false);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		isHover = true;
		closeButton.gameObject.SetActive(value: true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		isHover = false;
		closeButton.gameObject.SetActive(value: false);
	}
}
