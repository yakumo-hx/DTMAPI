using System;
using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown.UI;

public class TextPlayer
{
	public bool isPlaying { get; private set; }

	public TMP_Text text { get; private set; }

	public RectTransform textRect { get; private set; }

	public TextAnimatorPlayer textPlayer { get; private set; }

	public TextPlayer(TextAnimatorPlayer player, Action onTextShowed, UnityAction<char> onCharacterVisible = null)
	{
		TextPlayer textPlayer = this;
		this.textPlayer = player;
		text = this.textPlayer.GetComponent<TMP_Text>();
		textRect = this.textPlayer.GetComponent<RectTransform>();
		this.textPlayer.onTextShowed.AddListener(delegate
		{
			onTextShowed();
			textPlayer.isPlaying = false;
		});
		if (onCharacterVisible != null)
		{
			this.textPlayer.onCharacterVisible.AddListener(onCharacterVisible);
		}
	}

	public void Say(string content)
	{
		isPlaying = true;
		textPlayer.ShowText(content);
	}

	public void Skip()
	{
		textPlayer.SkipTypewriter();
	}

	public void Clear()
	{
		textPlayer.StopShowingText();
	}
}
