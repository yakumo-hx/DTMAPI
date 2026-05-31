using Febucci.UI;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class BubbleDialoguePanel : AutoSizeUIPanel, IDialogueLineView, IView
{
	[SerializeField]
	private TextMeshProUGUI nameText;

	[SerializeField]
	private TextMeshProUGUI holdPlaceText;

	[SerializeField]
	private TextMeshProUGUI contentText;

	[SerializeField]
	private TextAnimatorPlayer textPlayer;

	[SerializeField]
	private GameObject continueArrow;

	[SerializeField]
	private BubbleDialogPointer pointer;

	[SerializeField]
	private int maxNoWrapCharacters = 25;

	[SerializeField]
	private float minWidth = 156f;

	[SerializeField]
	private float preferredWidth = 576f;

	private int lastContentIndex;

	private int currentCharacterIndex;

	private string currentContent;

	private Vector2 nextTargetPositon;

	public bool isPlaying { get; private set; }

	public bool InRender => base.isRender;

	public bool InAnimation => base.inAnimation;

	public bool IsPlaying => isPlaying;

	protected override void __Init()
	{
		base.__Init();
		pointer.Init();
		textPlayer.onTextShowed.AddListener(delegate
		{
			isPlaying = false;
			continueArrow.SetActive(value: true);
		});
		textPlayer.onTypewriterStart.AddListener(delegate
		{
			isPlaying = true;
			continueArrow.SetActive(value: false);
		});
		textPlayer.onCharacterVisible.AddListener(OnCharacterVisible);
		base.displayAnimType = UiPanelDisplayAnimType.ScrollUp;
		base.displayAnimDuration = 0.5f;
		SetBoundaryPadding(32);
	}

	private void OnCharacterVisible(char c)
	{
		currentCharacterIndex++;
		if (!char.IsPunctuation(c) && currentCharacterIndex <= lastContentIndex)
		{
			DolocAPI.Sound.PostSoundEvent((currentCharacterIndex < lastContentIndex) ? SoundEvents.PLAY_NPC_TALK : SoundEvents.STOP_NPC_TALK);
		}
	}

	public void SetLineViewPosition(Vector2 worldPosition)
	{
		nextTargetPositon = worldPosition;
	}

	private void AdjustPosition(Vector2 targetPosition)
	{
		RebuildLayout();
		Vector2 adaptionPosition = GetAdaptionPosition(targetPosition);
		pointer.SetPosition((adaptionPosition - targetPosition).x, base.width);
		SetShowPosition(adaptionPosition);
	}

	public void WaitOption()
	{
	}

	public void Render(string npcName, string content)
	{
		nameText.text = npcName ?? string.Empty;
		PreResolveContent(content);
		currentContent = content;
		AdjustPosition(nextTargetPositon);
		Show();
	}

	private void PreResolveContent(string content)
	{
		currentCharacterIndex = 0;
		string text = content.ClearRichTextLabel();
		lastContentIndex = GetLastContentIndex(text);
		holdPlaceText.text = text;
		if (lastContentIndex <= maxNoWrapCharacters)
		{
			SetContentTextWidth(Mathf.Max(holdPlaceText.preferredWidth, minWidth));
		}
		else
		{
			SetContentTextWidth(preferredWidth);
		}
	}

	private void SetContentTextWidth(float width)
	{
		float y = holdPlaceText.rectTransform.sizeDelta.y;
		holdPlaceText.rectTransform.sizeDelta = new Vector2(width, y);
	}

	private int GetLastContentIndex(string content)
	{
		for (int num = content.Length - 1; num >= 0; num--)
		{
			if (!char.IsPunctuation(content[num]))
			{
				return num;
			}
		}
		return 0;
	}

	public void Skip()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_NPC_TALK);
		textPlayer.SkipTypewriter();
	}

	public void Pause()
	{
		Hide();
	}

	public void Resume()
	{
		Show(useTween: false);
		textPlayer.SkipTypewriter();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		contentText.text = string.Empty;
		base.gameObject.SetActive(value: true);
		textPlayer.ShowText(currentContent);
	}

	protected override void OnStartHide()
	{
		textPlayer.StopShowingText();
		continueArrow.SetActive(value: false);
		base.OnStartHide();
	}
}
