using System;
using Febucci.UI;
using UnityEngine;

namespace DolocTown.UI;

public class SceneDialogueBox : DolocUiRecyclableObject
{
	private TextPlayer textPlayer;

	private Vector2 wp;

	private Coroutine coroutine;

	public Action callback { get; set; }

	public float textAlpha
	{
		get
		{
			return textPlayer.text.alpha;
		}
		set
		{
			textPlayer.text.alpha = value;
		}
	}

	public bool isPlaying => textPlayer.isPlaying;

	protected override void __Init()
	{
		base.__Init();
		textPlayer = new TextPlayer(GetComponentInChildren<TextAnimatorPlayer>(), OnTextShowed);
		SetVisible(value: false);
	}

	private void OnTextShowed()
	{
		if (coroutine != null)
		{
			DolocAPI.StopCoroutine(coroutine);
		}
		callback?.Invoke();
		coroutine = DolocAPI.Delay(2f, delegate
		{
			SetVisible(value: false);
			coroutine = null;
		});
	}

	private void Update()
	{
		base.transform.position = DolocAPI.WorldToScreen(wp);
	}

	public void SetDialogShowPosition(Vector2 pos)
	{
		wp = pos;
		base.transform.position = DolocAPI.WorldToScreen(pos);
	}

	public void Say(string content)
	{
		SetVisible(value: true);
		textPlayer.Say(content);
	}

	public void Hide()
	{
		SetVisible(value: false);
	}

	public void test()
	{
		Init();
		SetDialogShowPosition(DolocAPI.AgentPosition);
		Say("Hello World!");
	}
}
