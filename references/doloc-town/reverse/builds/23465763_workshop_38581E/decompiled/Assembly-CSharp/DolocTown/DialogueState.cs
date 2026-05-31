using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DialogueState : DolocTownGameStateBase
{
	private DialoguePlayer dialoguePlayer;

	private bool showCandidateOptions;

	private int latestCandidateOption;

	private bool isWaitingForCandidateNote;

	private bool waitToHandleIdleTalk;

	private bool useCinemaScreen;

	public bool IgnoreCinemaScreenOnResumeOnce;

	private bool isStateActive;

	private bool isSpeedUp;

	private bool hasShowSpeedUpOperation;

	private RSTimer longPressConfirmTimer = new RSTimer(DolocAPI.GlobalParameter.UiButtonLongClickDuration);

	private RSTimer longPressSpeedUpTimer = new RSTimer(DolocAPI.GlobalParameter.UiButtonLongClickDuration);

	private DialogueNodeData currentNodeData;

	private string targetNpcName;

	private bool adjustAgentPosition;

	private bool agentInAnimation;

	public override bool ShowOutline => false;

	private DialogueManager dialogueManager => DolocAPI.archiveHandle.cityData.dialogueManager;

	private Queue<DialogueNodeData> unhandledDialogueNodes => dialogueManager.UnhandledDialogueNodes;

	public IDialogueEntity interactedNpc { get; private set; }

	private bool isInteractWithNpc => interactedNpc != null;

	public override void OnUpdate(float deltaTime)
	{
		if (!agentInAnimation && !ContinuouslyPressConfirm(deltaTime) && (!userInput.BaseIsCancelPressed || !dialoguePlayer.SelectThenFireClickExitOption()) && !ContinuouslyPressSpeedUp(deltaTime) && userInput.BaseToggleDialogueHistory && dialogueManager.historyManager.notEmpty)
		{
			DolocAPI.EnterUI<DialogueHistoryUiState>();
		}
	}

	public override void OnFixedUpdate(float deltaTime)
	{
		DolocAPI.gameStateManager.agentController.droneController.OnFixedUpdate(deltaTime);
	}

	private void EnableSpeedUp(bool value)
	{
		if (isSpeedUp)
		{
			hasShowSpeedUpOperation = true;
		}
		if (isSpeedUp != value)
		{
			isSpeedUp = value;
			if (value)
			{
				DolocAPI.SetTimeScale(DolocAPI.GlobalParameter.SpeedUpTimeScale);
			}
			else
			{
				DolocAPI.RevertTimeScale();
			}
		}
	}

	private bool ContinuouslyPressConfirm(float deltaTime)
	{
		bool startInput = userInput.BaseIsConfirmPressed || userInput.BaseContinueDialoguePressed || (userInput.DeviceType == DolocInputDeviceType.KeyboardMouse && userInput.BaseIsNextPressed);
		bool cancelInput = userInput.BaseIsConfirmInProgress || userInput.BaseContinueDialogueInProgress || (userInput.DeviceType == DolocInputDeviceType.KeyboardMouse && userInput.BaseIsNextInProgress);
		return ContinuouslyPress(deltaTime, () => startInput, () => cancelInput, delegate
		{
			dialoguePlayer.Continue(deltaTime, DolocAPI.GlobalParameter.HoldOnIntervalInDialogueNormal, startInput);
		}, longPressConfirmTimer);
	}

	private bool ContinuouslyPressSpeedUp(float deltaTime)
	{
		bool startInput = userInput.BaseIsCancelPressed || userInput.GlobalSpeedUpPressed;
		bool cancelInput = userInput.BaseIsCancelInProgress || userInput.GlobalSpeedUpInProgress;
		bool num = ContinuouslyPress(deltaTime, () => startInput, () => cancelInput, delegate
		{
			dialoguePlayer.Continue(deltaTime, DolocAPI.GlobalParameter.HoldOnIntervalInDialogueSpeedUp, startInput);
			EnableSpeedUp(value: true);
		}, longPressSpeedUpTimer);
		if (!num)
		{
			EnableSpeedUp(value: false);
		}
		return num;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		isStateActive = true;
		useCinemaScreen = isInteractWithNpc || (unhandledDialogueNodes.Count > 0 && dialogueManager.UseCinemaScreen(unhandledDialogueNodes.First().nodeName));
		if (useCinemaScreen)
		{
			EnableCinemaScreen();
		}
		agentInAnimation = true;
		latestCandidateOption = -1;
		DolocAPI.cameraController.setEnabled(value: false);
		if (!isInteractWithNpc)
		{
			DolocAPI.DelayFrame(DequeueUnhandledDialogue);
			return;
		}
		if (adjustAgentPosition)
		{
			AdjustAgentPosition(interactedNpc, delegate
			{
				SetAgentInCutscene(value: true);
			}).Forget();
		}
		showCandidateOptions = false;
		isWaitingForCandidateNote = false;
		HandleDialogueEntrance();
	}

	public void EnableCinemaScreen()
	{
		DolocAPI.SetBasicTipVisible(value: false);
		DolocAPI.SetQuickInventoryVisible(value: false);
		DolocAPI.SetSceneOperationTipEnabled(value: false);
		DolocAPI.SetPPM_CinemaScreen(value: true);
		DolocAPI.uiSystem.gameTimeTip.SetSpeedUpOperationTipVisible(value: true);
		DolocAPI.SetAllNpcAutoFlipState(value: false);
		DolocAPI.SetAllNpcEventState(value: false);
		DolocAPI.agent.SetToForeground(active: true);
		DolocAPI.SetDialogueTargetForeground(targetNpcName, active: true);
		DolocAPI.droneRenderer.SetDroneForeground(value: false);
		DolocAPI.Motor.SetMotorForeground(value: false);
	}

	private void HandleDialogueEntrance()
	{
		if (dialogueManager.GetEntrance(targetNpcName, out var entranceNode))
		{
			showCandidateOptions = true;
			dialoguePlayer.MarkReplayableNode();
			PlayDialogue(entranceNode, targetNpcName);
		}
		else
		{
			HandleCandidateDialogueNodes();
		}
	}

	private void HandleIdleTalkNodes()
	{
		if (dialogueManager.GetRandomIdleTalkNode(targetNpcName, out var idleTalkNode, out var _))
		{
			DolocAPI.DelayFrame(delegate
			{
				PlayDialogue(idleTalkNode, targetNpcName);
			});
		}
		else
		{
			DequeueUnhandledDialogue();
		}
	}

	private void HandleCandidateDialogueNodes()
	{
		showCandidateOptions = false;
		isWaitingForCandidateNote = true;
		string[] candidateLines = dialogueManager.GetLocalizedCandidateLines(targetNpcName);
		if (!candidateLines.IsNullOrEmpty())
		{
			dialoguePlayer.SetSelectOptionCallback(delegate(int index)
			{
				latestCandidateOption = index;
				isWaitingForCandidateNote = false;
				dialoguePlayer.SetSelectOptionCallback(null);
				dialoguePlayer.HideOptionView();
				if (!dialogueManager.GetCandidateNodeByIndex(targetNpcName, index, out var nodeName))
				{
					Debug.LogError($"获取候选对话节点失败 {targetNpcName} - {index}");
					PopState();
				}
				else
				{
					DolocAPI.AppendDialogueHistoryOption(candidateLines[index]);
					PlayDialogue(nodeName, targetNpcName);
				}
			});
			dialoguePlayer.ShowOptionWithLines(candidateLines, latestCandidateOption);
		}
		else
		{
			DequeueUnhandledDialogue();
		}
	}

	private void PlayDialogue(DialogueNodeData nodeData)
	{
		PlayDialogue(nodeData.nodeName, nodeData.npcName);
	}

	private void PlayDialogue(string nodeName, string npcName)
	{
		dialogueManager.Visit(nodeName);
		if (dialoguePlayer.IsDialogueRunning)
		{
			dialoguePlayer.Stop();
		}
		currentNodeData = new DialogueNodeData(nodeName, npcName);
		if (isInteractWithNpc && !dialogueManager.IsResidentNode(nodeName))
		{
			DolocAPI.RemoveDialogueNode(nodeName, npcName);
		}
		if (!dialoguePlayer.StartDialogue(nodeName, npcName, OnDialogueComplete))
		{
			Debug.LogError("对话节点<" + currentNodeData.nodeName + ">@<" + currentNodeData.npcName + ">播放异常");
			DolocAPI.DelayFrame(PopState);
		}
	}

	private void OnDialogueComplete()
	{
		if (isInteractWithNpc)
		{
			if (waitToHandleIdleTalk)
			{
				HandleIdleTalkNodes();
				waitToHandleIdleTalk = false;
				showCandidateOptions = true;
				return;
			}
			if (showCandidateOptions)
			{
				HandleCandidateDialogueNodes();
				showCandidateOptions = false;
				return;
			}
		}
		DolocAPI.DelayFrame(DequeueUnhandledDialogue);
	}

	private void DequeueUnhandledDialogue()
	{
		interactedNpc = null;
		if (unhandledDialogueNodes.TryDequeue(out var data))
		{
			dialoguePlayer.MarkReplayableNode();
			WaitToDisableAgent(delegate
			{
				if (!useCinemaScreen && dialogueManager.UseCinemaScreen(data.nodeName))
				{
					EnableCinemaScreen();
				}
				PlayDialogue(data);
				DolocAPI.SetDialogueTargetForeground(data.npcName, active: true);
			}).Forget();
		}
		else
		{
			PopState();
		}
	}

	private void PopState()
	{
		dialoguePlayer.Stop();
		gameController.WaitToPopState(this);
	}

	public override void OnExit()
	{
		base.OnExit();
		isStateActive = false;
		DolocAPI.DelayFrame(DolocAPI.RevertTimeScale);
		isSpeedUp = false;
		hasShowSpeedUpOperation = false;
		DolocAPI.SetSceneOperationTipEnabled(value: true);
		DolocAPI.cameraController.setEnabled(value: true);
		SetCinemaScreenState(value: false);
		SetAgentInCutscene(value: false);
		DolocAPI.SetAllNpcAutoFlipState(value: true);
		DolocAPI.SetAllNpcEventState(value: true);
		DolocAPI.uiSystem.dialogueHistoryTip.Hide();
		DolocAPI.RevertAllDialogueEntitiesSortingOrder();
		DolocAPI.droneRenderer.SetDroneForeground(value: true);
		DolocAPI.Motor.SetMotorForeground(value: true);
		adjustAgentPosition = false;
		currentNodeData = default(DialogueNodeData);
		interactedNpc = null;
		useCinemaScreen = true;
		agentInAnimation = false;
		waitToHandleIdleTalk = false;
		IgnoreCinemaScreenOnResumeOnce = false;
	}

	public override void OnResume()
	{
		DolocAPI.SetSceneOperationTipEnabled(value: false);
		if (!IgnoreCinemaScreenOnResumeOnce)
		{
			SetCinemaScreenState(value: true);
		}
		IgnoreCinemaScreenOnResumeOnce = false;
		dialoguePlayer.Resume();
		DolocAPI.uiSystem.dialogueHistoryTip.TryShow();
	}

	public override void OnPause()
	{
		DolocAPI.RevertTimeScale();
		DolocAPI.DelayFrame(DolocAPI.RevertTimeScale);
		SetCinemaScreenState(value: false);
		dialoguePlayer.Pause();
		DolocAPI.uiSystem.dialogueHistoryTip.Hide();
	}

	private void SetCinemaScreenState(bool value)
	{
		if (!value)
		{
			DolocAPI.SetPPM_CinemaScreen(value: false);
		}
		else if (useCinemaScreen)
		{
			DolocAPI.SetPPM_CinemaScreen(value: true);
		}
	}

	public DialogueState(GameStateMachine userInput)
		: base(userInput, DolocInputType.BASE, shouldPauseGame: true, shouldLateUpdate: false, supportCutscenes: false)
	{
		DialogueOptionPanel entity = DolocAPI.uiSystem.GetEntity<DialogueOptionPanel>();
		dialoguePlayer = new DialoguePlayer(entity);
	}

	public bool DialogueWithNpc(string npcName, IDialogueEntity targetView = null, bool adjustAgentPosition = true)
	{
		if (string.IsNullOrEmpty(npcName))
		{
			Debug.LogWarning("未指定npcName");
			return false;
		}
		if (targetView == null)
		{
			targetView = DolocAPI.GetDialogueTargetViewOrDefault(npcName);
		}
		if (targetView == null)
		{
			return false;
		}
		if (!dialogueManager.CheckDialogueStatus(npcName))
		{
			Debug.LogWarning("<" + npcName + ">暂无对话");
			return false;
		}
		targetNpcName = npcName;
		this.adjustAgentPosition = adjustAgentPosition;
		interactedNpc = targetView;
		DolocAPI.BroadcastString(GameEventType.TALK_WITH_NPC, npcName);
		if (gameController.CurrentState != this)
		{
			Startup();
		}
		else
		{
			OnEnter();
		}
		return true;
	}

	public bool AppendDialogue(string nodeName, string npcName)
	{
		if (string.IsNullOrEmpty(nodeName) || !dialogueManager.NodeExists(nodeName))
		{
			return false;
		}
		unhandledDialogueNodes.Enqueue(new DialogueNodeData(nodeName, npcName));
		if (gameController.CurrentState != this)
		{
			DolocAPI.WaitUntil(() => DolocAPI.IsCurrentStateSupportCutscenes, delegate
			{
				if (DolocAPI.IsAgentRiding)
				{
					DolocAPI.gameStateManager.agentController.GetOffMotor();
				}
				Startup();
			});
		}
		return true;
	}

	public void ReturnToCandidates()
	{
		if (!isInteractWithNpc)
		{
			Debug.LogError("当前没有与npc进行对话");
		}
		else
		{
			showCandidateOptions = true;
		}
	}

	public void HandleIdleTalk()
	{
		if (!isInteractWithNpc)
		{
			Debug.LogError("当前没有与npc进行对话");
		}
		else
		{
			waitToHandleIdleTalk = true;
		}
	}

	public void QuitDialogue()
	{
		PopState();
	}

	public void IgnoreNextOptionVisit()
	{
		dialoguePlayer.IgnoreNextOptionVisit = true;
	}

	private bool CheckRaycastGroundMask(Vector2 origin, Vector2 dir, float distance)
	{
		return Physics2D.Raycast(origin, dir, distance, DolocAPI.gameConfig.groundMask).collider != null;
	}

	private async UniTaskVoid AdjustAgentPosition(IDialogueEntity npc, Action onAnimEnd, int offsetCell = 2)
	{
		ResetAgentVelocity();
		await UniTask.WaitUntil(() => DolocAPI.IsCurrentStateSupportInteract);
		Vector2 npcPos = npc.WorldPosition;
		int dir = ((!(DolocAPI.AgentPosition.x - npcPos.x < 0f)) ? 1 : (-1));
		Vector2 offset = new Vector2(Mathf.Abs(offsetCell), 0f) * ((float)dir * 1.5f);
		float wallRaycastDistance = (float)(Mathf.Abs(offsetCell) + 1) * 1.5f;
		float groundRaycastDistance = 0.15f;
		float agentHeightHalf = DolocAPI.agent.PositionCenter.y - DolocAPI.agent.position2d.y;
		if (!DolocAPI.archiveHandle.currentRoom.Geometry.IsValidPositionX((npcPos + offset).x, 1.5f) || !CheckWallValid() || !CheckGroundValid())
		{
			dir *= -1;
			offset *= -1f;
		}
		if (!CheckWallValid() || !CheckGroundValid())
		{
			AnimEnd();
			return;
		}
		Vector2 vector = npcPos + offset;
		if (!DolocAPI.archiveHandle.currentRoom.Geometry.IsValidPositionX(vector.x))
		{
			vector.x = DolocAPI.AgentPosition.x;
		}
		vector.y = DolocAPI.AgentPosition.y;
		if (!npc.DisableActing)
		{
			npc.LookAt(vector.x);
		}
		if ((vector - (Vector2)DolocAPI.AgentPosition).magnitude < 0.3f)
		{
			AnimEnd();
			return;
		}
		await DolocAPI.AgentRenderer.WalkTo(vector, 5f);
		AnimEnd();
		void AnimEnd()
		{
			DolocAPI.AgentRenderer.LookAt(npcPos);
			onAnimEnd?.Invoke();
		}
		bool CheckGroundValid()
		{
			if (DolocAPI.CurrentRoom.IsInHouse)
			{
				return true;
			}
			Vector2 origin = new Vector2((npcPos + offset).x + (float)dir * 1.5f, DolocAPI.agent.PositionCenter.y);
			return CheckRaycastGroundMask(origin, Vector2.down, agentHeightHalf + groundRaycastDistance);
		}
		bool CheckWallValid()
		{
			Vector2 origin2 = new Vector2(npcPos.x, DolocAPI.agent.PositionCenter.y);
			return !CheckRaycastGroundMask(origin2, new Vector2(dir, 0f), wallRaycastDistance);
		}
	}

	private async UniTaskVoid WaitToDisableAgent(Action callBack = null)
	{
		ResetAgentVelocity();
		await UniTask.Delay(50);
		await UniTask.WaitUntil(() => DolocAPI.IsCurrentStateSupportInteract);
		DolocAPI.agent.SetToForeground(active: true);
		if (isStateActive)
		{
			SetAgentInCutscene(value: true);
		}
		callBack?.Invoke();
	}

	private void ResetAgentVelocity()
	{
		DolocAPI.agent.ResetVelocity();
	}

	private void SetAgentInCutscene(bool value)
	{
		agentInAnimation = false;
		DolocAPI.agent.InCutscene = value;
		DolocAPI.agent.ResetCollider();
	}
}
