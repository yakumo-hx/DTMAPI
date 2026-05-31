using System;
using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EnvOptimizerConsole : DolocUiObject, IScrollContentRect
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private RectTransform layoutGroupRectTransform;

	[SerializeField]
	private TextMeshProUGUI fixedText;

	[SerializeField]
	private TextMeshProUGUI appendText;

	[SerializeField]
	private TextAnimatorPlayer textAnimatorPlayer;

	[SerializeField]
	private ScrollRect _scrollRect;

	private Action onEnd;

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		textAnimatorPlayer.onCharacterVisible.AddListener(delegate
		{
			RefreshLayout(refreshFixedContent: false);
		});
		textAnimatorPlayer.onTextShowed.AddListener(delegate
		{
			onEnd?.Invoke();
			onEnd = null;
		});
	}

	public void Render(EnvOptimizerData data)
	{
		title.text = base.staticTexts.UiEnvOptimizerConsoleTitle;
		fixedText.text = data.consoleText;
		textAnimatorPlayer.ShowText("");
		LayoutRebuilder.ForceRebuildLayoutImmediate(fixedText.rectTransform);
		RefreshLayout(refreshFixedContent: true);
	}

	public void AppendContent(EnvOptimizerConsoleBlockData data, Action onEnd)
	{
		RefreshLayout(refreshFixedContent: true);
		textAnimatorPlayer.ShowText("\n" + data.content);
		this.onEnd = onEnd;
	}

	private void RefreshLayout(bool refreshFixedContent)
	{
		if (refreshFixedContent)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(fixedText.rectTransform);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(appendText.rectTransform);
		LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroupRectTransform);
		scrollRect.verticalScrollbar.value = 0f;
	}

	public void OnMove()
	{
	}
}
