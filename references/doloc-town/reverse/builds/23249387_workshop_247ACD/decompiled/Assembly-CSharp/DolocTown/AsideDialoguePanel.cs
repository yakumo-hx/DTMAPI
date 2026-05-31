using DolocTown.UI;
using Febucci.UI;
using TMPro;
using UnityEngine;

namespace DolocTown;

public class AsideDialoguePanel : DolocUIPanel, IDialogueLineView, IView
{
	[SerializeField]
	private TMP_Text contentText;

	[SerializeField]
	private TextAnimatorPlayer textPlayer;

	private int currentCharacterIndex;

	private string currentContent;

	public bool isPlaying { get; private set; }

	public bool InRender => base.isRender;

	public bool InAnimation => base.inAnimation;

	public bool IsPlaying => isPlaying;

	protected override void __Init()
	{
		base.__Init();
		textPlayer.onTextShowed.AddListener(delegate
		{
			isPlaying = false;
		});
		textPlayer.onTypewriterStart.AddListener(delegate
		{
			isPlaying = true;
		});
	}

	public void SetLineViewPosition(Vector2 worldPosition)
	{
	}

	public void WaitOption()
	{
		Hide();
	}

	public void Render(string npcName, string content)
	{
		currentContent = content;
		Show();
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
		base.OnStartHide();
	}
}
