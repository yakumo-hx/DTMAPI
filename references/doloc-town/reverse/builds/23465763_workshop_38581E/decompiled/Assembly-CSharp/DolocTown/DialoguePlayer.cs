using System.Collections.Generic;
using System.Linq;
using DolocTown.UI;
using RedSaw;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class DialoguePlayer
{
	private struct NpcCache
	{
		public readonly string Id;

		private string _title;

		public bool IsVisited { get; private set; }

		public string Title
		{
			get
			{
				if (!IsVisited || string.IsNullOrEmpty(_title))
				{
					_title = DolocAPI.GetNpcTitle(Id);
				}
				return _title;
			}
		}

		public NpcCache(string npcId)
		{
			Id = npcId;
			IsVisited = DolocAPI.IsVisitedNpcName(Id);
			_title = null;
		}
	}

	private IDialogueOptionView optionsView;

	private IDialogueLineView currentlineView;

	private UnityAction onDialogueComplete;

	private bool useSayAnim;

	private RSTimer holdOnTimer = new RSTimer();

	private Dictionary<string, NpcCache> npcCaches = new Dictionary<string, NpcCache>();

	private IDialogueEntity currentNpc;

	private Vector2 lastPopPosition = Vector2.zero;

	private HashSet<string> optionCache = new HashSet<string>();

	public bool IgnoreNextOptionVisit;

	private bool shouldResumeLineView;

	private bool shouldResumeOptionView;

	private OptionGroupData currentOptionGroupData;

	private bool shouldHoldOn;

	private DialogueManager dialogueManager => DolocAPI.archiveHandle.cityData.dialogueManager;

	public bool IsDialogueRunning => dialogueManager.IsDialogueRunning;

	public DialoguePlayer(IDialogueOptionView optionsView)
	{
		this.optionsView = optionsView;
		holdOnTimer = new RSTimer(DolocAPI.GlobalParameter.HoldOnIntervalInDialogueNormal);
	}

	public bool StartDialogue(string nodeName, string defaultNpcName, UnityAction onDialogueComplete)
	{
		Debug.Log("Start " + nodeName);
		this.onDialogueComplete = onDialogueComplete;
		useSayAnim = true;
		optionCache.Clear();
		Register();
		currentlineView = DolocAPI.uiSystem.GetEntity<BubbleDialoguePanel>();
		return dialogueManager.StartDialogue(nodeName, defaultNpcName);
	}

	public void MarkReplayableNode()
	{
		dialogueManager.historyManager.MarkReplayableBlock();
	}

	private void Register()
	{
		dialogueManager.OnLineNeedsPresentation.AddListener(RenderTextLine);
		dialogueManager.OnOptionsNeedPresentation.AddListener(RenderOptions);
		dialogueManager.OnNodeStart.AddListener(HandleNodeStart);
		dialogueManager.OnSetSelectedOption.AddListener(HandleSetSelectedOption);
		dialogueManager.OnDialogueComplete.AddListener(HandleDialogueComplete);
		dialogueManager.OnAsyncTaskStarted.AddListener(HandleWaitingStart);
	}

	private void Unregister()
	{
		dialogueManager.OnLineNeedsPresentation.RemoveListener(RenderTextLine);
		dialogueManager.OnOptionsNeedPresentation.RemoveListener(RenderOptions);
		dialogueManager.OnNodeStart.RemoveListener(HandleNodeStart);
		dialogueManager.OnSetSelectedOption.RemoveListener(HandleSetSelectedOption);
		dialogueManager.OnDialogueComplete.RemoveListener(HandleDialogueComplete);
		dialogueManager.OnAsyncTaskStarted.RemoveListener(HandleWaitingStart);
	}

	private void HandleNodeStart(string nodeName)
	{
		useSayAnim = dialogueManager.UseTalkAnim(nodeName);
	}

	private void HandleDialogueComplete()
	{
		HideView();
		Unregister();
		onDialogueComplete?.Invoke();
		onDialogueComplete = null;
	}

	private void HandleWaitingStart(string npcName)
	{
		currentNpc?.StopSay();
		currentNpc = null;
		HideView();
	}

	private void HandleSetSelectedOption(string lineId)
	{
		if (!dialogueManager.DisableTrackingOption(dialogueManager.CurrentNode))
		{
			optionCache.Add(lineId);
		}
	}

	private void PauseView()
	{
		if (currentlineView != null)
		{
			shouldResumeLineView = currentlineView.InRender;
			currentlineView.Pause();
		}
		if (optionsView != null)
		{
			shouldResumeOptionView = optionsView.InRender;
			optionsView.Pause();
		}
	}

	private void ResumeView()
	{
		if (shouldResumeLineView && currentlineView != null)
		{
			currentlineView.Resume();
		}
		if (shouldResumeOptionView && optionsView != null)
		{
			optionsView.Resume();
		}
		shouldResumeLineView = false;
		shouldResumeOptionView = false;
	}

	private void HideView()
	{
		currentlineView?.Hide();
		optionsView?.Hide();
	}

	private bool TryGetCacheData(string npcName, out NpcCache data)
	{
		if (string.IsNullOrEmpty(npcName))
		{
			data = default(NpcCache);
			return false;
		}
		if (!npcCaches.TryGetValue(npcName, out data))
		{
			npcCaches[npcName] = new NpcCache(npcName);
		}
		data = npcCaches[npcName];
		return true;
	}

	private void RenderTextLine(string npcName, LocalizedLine line)
	{
		if (line == null)
		{
			dialogueManager.Continue();
			return;
		}
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(npcName);
		IDialogueLineView dialogueLineView = DolocAPI.GetDialogueLineView(npcName);
		if (dialogueTargetViewOrDefault is NoneDialogueTarget || dialogueLineView == null)
		{
			dialogueManager.Continue();
			return;
		}
		if (currentlineView != dialogueLineView)
		{
			currentlineView?.Hide();
			currentlineView = dialogueLineView;
		}
		if (currentNpc != dialogueTargetViewOrDefault)
		{
			if (useSayAnim)
			{
				currentNpc?.StopSay();
			}
			currentNpc = dialogueTargetViewOrDefault;
			if (useSayAnim)
			{
				currentNpc?.StartSay();
			}
		}
		Vector2 lineViewPosition = lastPopPosition;
		if (dialogueTargetViewOrDefault != null)
		{
			lineViewPosition = (lastPopPosition = dialogueTargetViewOrDefault.UiPopPosition);
		}
		currentlineView.SetLineViewPosition(lineViewPosition);
		TryGetCacheData(npcName, out var data);
		string content = DolocAPI.GetTextWithMarkup(line.TextWithoutCharacterName);
		currentlineView.Render(data.Title, content);
		DolocAPI.DelayFrame(delegate
		{
			DolocAPI.AppendDialogueHistoryLine(data.Id, data.Title, content);
		});
	}

	private void RenderOptions(DialogueOption[] options)
	{
		DialogueOption[] array = options.Where((DialogueOption x) => x.IsAvailable).ToArray();
		if (array.Length == 0)
		{
			Debug.LogWarning("当前选项均不可达，结束当前对话！");
			Stop();
			return;
		}
		currentlineView?.WaitOption();
		int num;
		if (IgnoreNextOptionVisit)
		{
			num = 0;
		}
		else
		{
			num = int.MaxValue;
			foreach (DialogueOption dialogueOption in options)
			{
				dialogueOption.IsVisited = optionCache.Contains(dialogueOption.TextID);
				if (!dialogueOption.IsVisited)
				{
					num = Mathf.Min(num, dialogueOption.DialogueOptionID);
				}
			}
			if (num >= options.Length)
			{
				num = 0;
			}
		}
		IgnoreNextOptionVisit = false;
		SetSelectOptionCallback(OnOptionButtonClick);
		currentOptionGroupData = new OptionGroupData(array);
		optionsView.Render(currentOptionGroupData);
		optionsView.Select(num);
	}

	private void OnOptionButtonClick(int index)
	{
		dialogueManager.SetSelectedOption(index);
		if (currentOptionGroupData.notEmpty)
		{
			OptionData[] array = currentOptionGroupData.options.Where((OptionData x) => x.index == index).ToArray();
			if (array.Length != 0)
			{
				DolocAPI.AppendDialogueHistoryOption(array[0].text);
			}
		}
		optionsView.Hide();
		SetSelectOptionCallback(null);
	}

	public void Continue(float deltaTime, float interval, bool isTap)
	{
		if (!IsDialogueRunning || currentlineView.InAnimation || optionsView.InAnimation)
		{
			return;
		}
		if (interval > 0f)
		{
			holdOnTimer.Clamp(interval);
			if (!isTap && shouldHoldOn && !holdOnTimer.Tick(deltaTime))
			{
				return;
			}
			shouldHoldOn = false;
		}
		if (currentlineView.IsPlaying)
		{
			shouldHoldOn = true;
			holdOnTimer.SetInterval(interval);
			currentlineView.Skip();
		}
		else
		{
			holdOnTimer.SetInterval(interval);
			dialogueManager.Continue();
		}
	}

	public void Stop()
	{
		if (useSayAnim)
		{
			currentNpc?.StopSay();
		}
		currentNpc = null;
		Unregister();
		onDialogueComplete = null;
		dialogueManager.StopDialogue();
		HideView();
	}

	public void Pause()
	{
		if (useSayAnim)
		{
			currentNpc?.StopSay();
		}
		currentNpc = null;
		PauseView();
	}

	public void Resume()
	{
		ResumeView();
	}

	public void SetSelectOptionCallback(UnityAction<int> callback)
	{
		optionsView.SetClickCallbacks(callback);
	}

	public void HideLineView()
	{
		currentlineView?.Hide();
	}

	public void HideOptionView()
	{
		optionsView.Hide();
	}

	public void ShowOptionWithLines(string[] lines, int dataIndex)
	{
		optionsView.Render(new OptionGroupData(lines));
		optionsView.SelectByDataIndex(dataIndex);
	}

	public bool SelectThenFireClickExitOption()
	{
		return optionsView.SelectThenFireClickExitOption();
	}
}
