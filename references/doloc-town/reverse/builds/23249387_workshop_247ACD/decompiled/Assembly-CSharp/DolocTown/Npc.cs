using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.NPC;
using DolocTown.Config.Room;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class Npc
{
	public readonly NpcInfo proto;

	public readonly Vector2 emotionOffset;

	[JsonProperty]
	public string aliasId;

	private NpcRenderer _renderer;

	public readonly NpcController controller;

	[DebugInfo("被呼叫解除计数器")]
	private Counter invokedCounter;

	private HashSet<string> _seenNpcs = new HashSet<string>();

	public bool? overrideVisible;

	public bool? overrideFaceLeft;

	public bool disableFreeActing;

	public bool disableEventFlag;

	[DebugInfo("现在或者刚才要前往的目标")]
	private string _currentJourneyInfo;

	public int SortingOrder => proto.SortingOrder;

	[JsonProperty]
	public string NpcName => proto.Id;

	public string OriginTitle => proto.Title;

	public RuntimeAnimatorController RuntimeAnimatorController => proto.Animator;

	[DebugInfo("当前世界坐标")]
	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	public Vector2 positionWS { get; private set; }

	[DebugInfo("是否存在事件")]
	public bool HasEventNow
	{
		get
		{
			if (!disableEventFlag)
			{
				return DolocAPI.CheckDialogueEvent(NpcName);
			}
			return false;
		}
	}

	public NpcRenderer Renderer
	{
		get
		{
			return _renderer;
		}
		set
		{
			if (value == null)
			{
				UnRenderNpc();
			}
			else
			{
				RenderNpc(value);
			}
		}
	}

	public NpcTaskRenderController TaskRenderController { get; private set; }

	[DebugInfo("是否正在渲染")]
	public bool IsRenderNow { get; protected set; }

	[DebugInfo("是否被玩家呼叫")]
	public bool isInvoked { get; private set; }

	[DebugInfo("呼叫对象")]
	public GameObject invokedTarget { get; private set; }

	public bool isValid { get; private set; }

	public bool isForegroundInDialogue { get; set; }

	[DebugInfo("当前场景名称")]
	[JsonProperty]
	public string sceneName { get; private set; }

	[JsonProperty]
	public bool hasKnownName { get; private set; }

	[DebugInfo("是否处于虚空场景")]
	public bool IsAtVoidScene => string.IsNullOrEmpty(sceneName);

	[DebugInfo("覆盖计划表的目标点")]
	[JsonProperty]
	public string overrideMarkPoint { get; private set; }

	[DebugInfo("当前是否在执行计划表")]
	public bool useSchedule => overrideMarkPoint.IsNullOrEmpty();

	[DebugInfo("是否正在移动")]
	public bool isMoving => controller.CurrentTaskType == typeof(NpcTaskMove);

	public bool isWorking => typeof(NpcWorkTask).IsAssignableFrom(controller.CurrentTaskType);

	[DebugInfo("是否正在执行站街任务")]
	public bool IsInStreetMission => controller.CurrentTaskType == typeof(NpcWorkTaskStreet);

	public string NpcStatusInfo
	{
		get
		{
			string text = "暂无工作";
			if (controller.CurrentTaskType == typeof(NpcWorkTaskStreet))
			{
				text = "站街中..";
			}
			else if (controller.CurrentTaskType == typeof(NpcTaskMove))
			{
				text = "前往目标场景:" + _currentJourneyInfo;
			}
			return "Npc:\"" + proto.Id + "\"\n" + $"是否执行工作计划表:{useSchedule}\n" + $"是否存在独特事件:{HasEventNow}\n" + $"是否被玩家呼叫:{isInvoked}\n" + $"正在执行:\"{controller.CurrentTaskType}\"\n" + text;
		}
	}

	public string GetCurrentTitle(bool ignoreUnknown)
	{
		if (!aliasId.IsNullOrEmpty() && proto.Alias_Index.TryGetValue(aliasId, out var value))
		{
			return value.Text;
		}
		if (!ignoreUnknown && !hasKnownName)
		{
			return DolocConfig.StaticTexts.UiOperationTalkUnknown;
		}
		return OriginTitle;
	}

	public Npc(NpcInfo proto)
	{
		this.proto = proto;
		Renderer = null;
		controller = new NpcController(this);
		ManualSetToMarkPoint(proto.InitialMarkPoint_Ref);
		if (!proto.ScheduleInitialState)
		{
			overrideMarkPoint = proto.InitialMarkPoint;
		}
		emotionOffset = proto.EmotionOffset;
		TaskRenderController = this.CreateNpcTaskController();
		invokedCounter = new Counter(proto.InvokeDuration);
	}

	[JsonConstructor]
	public Npc(string npcName, Vector2 positionWS, bool hasKnownName, string sceneName, bool shouldRunSchedule, string aliasId, string overrideMarkPoint)
	{
		proto = DolocConfig.Tables.TbNpc.GetOrDefault(npcName);
		if (proto != null)
		{
			this.sceneName = sceneName;
			this.hasKnownName = hasKnownName;
			this.positionWS = positionWS;
			controller = new NpcController(this);
			emotionOffset = proto.EmotionOffset;
			this.aliasId = aliasId;
			this.overrideMarkPoint = overrideMarkPoint;
			TaskRenderController = this.CreateNpcTaskController();
			invokedCounter = new Counter(proto.InvokeDuration);
			isValid = true;
		}
	}

	public void OnPlayerSceneChanged(string sceneName)
	{
		_SetRendererStatusFromCurrentScene(sceneName);
	}

	private void _SetRendererStatusFromCurrentScene(string agentSceneName)
	{
		if (CompareSceneName(agentSceneName) && !IsAtVoidScene)
		{
			if (!IsRenderNow && (!overrideVisible.HasValue || overrideVisible.Value))
			{
				Renderer = DolocAPI.EntitySystem.Next<NpcRenderer>();
			}
		}
		else if (IsRenderNow)
		{
			Renderer = null;
		}
	}

	private void _SetRendererStatusFromCurrentScene()
	{
		string agentSceneName = (DolocAPI.archiveHandle?.currentRoom)?.SceneRawName ?? string.Empty;
		_SetRendererStatusFromCurrentScene(agentSceneName);
	}

	private void RenderNpc(NpcRenderer renderer)
	{
		_renderer = renderer;
		_renderer.npc = this;
		IsRenderNow = true;
		_renderer.SetNpcManualSortingOrder(SortingOrder);
		_renderer.positionWS = positionWS;
		_renderer.animatorController = proto.Animator;
		_renderer.GetComponent<EmotionPositionControl>().EmotionOffset = emotionOffset;
		_renderer.PlayAnimation("idle", force: false);
		SetToForeground(isForegroundInDialogue);
		RefreshNpcEventStatus();
		TaskRenderController.OnRenderNpc();
		if (overrideFaceLeft.HasValue)
		{
			_renderer.FaceLeft(overrideFaceLeft.Value);
		}
	}

	private void UnRenderNpc()
	{
		isInvoked = false;
		IsRenderNow = false;
		this.HideSceneOperationTip();
		TaskRenderController?.OnUnRenderNpc();
		if (_renderer != null)
		{
			DolocAPI.ClearEmotions(_renderer.transform);
			_renderer.StopWalk();
			_renderer.mover.ResetMover();
			_renderer.npc = null;
			DolocAPI.EntitySystem.Recycle(_renderer);
			_renderer = null;
		}
	}

	public void SetToForeground(bool active)
	{
		isForegroundInDialogue = active;
		bool flag = DolocAPI.CurrentRoom != null && sceneName == DolocAPI.CurrentRoom.SceneRawName && DolocAPI.CurrentRoom.HasResource;
		_renderer?.SetToForeground(active && flag);
	}

	public void UpdateAnimationByMoving()
	{
		if (!(_renderer == null) && isMoving && !isInvoked)
		{
			_renderer.PlayAnimation("walk", force: false);
		}
	}

	public void Invoke(GameObject who)
	{
		if (!(_renderer == null))
		{
			isInvoked = true;
			invokedTarget = who;
			invokedCounter.Reset();
			_renderer.mover.StopMove();
			if (isWorking)
			{
				controller.StopTask();
			}
			if (proto.DisableActing)
			{
				_renderer.FaceLeft(value: false);
			}
		}
	}

	public void CancelInvoke()
	{
		isInvoked = false;
		invokedTarget = null;
	}

	public void ClearSeenNpcs()
	{
		_seenNpcs.Clear();
	}

	public void OnInteract()
	{
		if (!DolocAPI.archiveHandle.cityData.dialogueManager.IsDialogueRunning)
		{
			Invoke(DolocAPI.agent.gameObject);
			DolocAPI.archiveHandle.cityData.likingManager.AddLikingByGreeting(NpcName);
			if (!DolocAPI.gameStateManager.dialogState.DialogueWithNpc(NpcName, Renderer))
			{
				this.PushSceneOperationTip();
				DolocAPI.RaiseEmotion(Renderer.transform, EmotionName.NOCOMMENT);
				DolocAPI.outputWarning("npc<" + NpcName + ">暂无对话");
			}
			DialogueState dialogState = DolocAPI.gameStateManager.dialogState;
			dialogState.DisposableExit = (Action)Delegate.Combine(dialogState.DisposableExit, new Action(ShowSceneOperationTip));
			this.PushSceneOperationTipToHide();
		}
	}

	public void ForceInteract()
	{
		Invoke(DolocAPI.agent.gameObject);
		DolocAPI.archiveHandle.cityData.likingManager.AddLikingByGreeting(NpcName);
		if (!DolocAPI.gameStateManager.dialogState.DialogueWithNpc(NpcName, Renderer))
		{
			this.PushSceneOperationTip();
			DolocAPI.RaiseEmotion(Renderer.transform, EmotionName.NOCOMMENT);
			DolocAPI.outputWarning("npc<" + NpcName + ">暂无对话");
		}
		DialogueState dialogState = DolocAPI.gameStateManager.dialogState;
		dialogState.DisposableExit = (Action)Delegate.Combine(dialogState.DisposableExit, new Action(ShowSceneOperationTip));
	}

	public void OnDisTouch()
	{
		Renderer.ShowOutline = false;
		this.HideSceneOperationTip();
	}

	public void OnTouch()
	{
		Renderer.ShowOutline = true;
		ShowSceneOperationTip();
	}

	private void ShowSceneOperationTip()
	{
		if (IsRenderNow)
		{
			this.ShowSceneOperationTip(() => (!(Renderer == null)) ? Renderer.operationPopPosition : default(Vector2), DolocAPI.GetNpcTitle(NpcName), DolocAPI.UserInput.GlobalInteractActionName);
		}
	}

	public void OnSeeOtherNpc(Npc other)
	{
		if (!_seenNpcs.Add(other.proto.Id) || disableFreeActing || proto.DisableActing || !controller.CheckTaskType<NpcWorkTaskStreet>())
		{
			return;
		}
		Invoke(other.Renderer.gameObject);
		DolocAPI.RaiseEmotion(_renderer.transform, EmotionName.NOTE);
		other.Invoke(Renderer.gameObject);
		UniTask.Delay(1000).ContinueWith(delegate
		{
			if (other.Renderer != null)
			{
				DolocAPI.RaiseEmotion(other.Renderer.transform, RandomUtils.Dice(0.5f) ? EmotionName.LOVE : EmotionName.NOCOMMENT);
			}
		}).Forget();
	}

	public void OnSeeAgent()
	{
	}

	public void ForceSetPosition(Vector2 positionWS)
	{
		ManualSetPositionToAgentScene(positionWS);
	}

	public void AfterPassTime()
	{
		if (IsRenderNow)
		{
			_renderer.positionWS = positionWS;
			_renderer.animatorController = proto.Animator;
			_renderer.PlayAnimation(controller.CurrentAnimationName, force: false);
			RefreshNpcEventStatus();
			_renderer.SetNpcManualSortingOrder(SortingOrder);
		}
	}

	public void UpdatePerSec()
	{
		if (isInvoked)
		{
			if (invokedCounter.Tick())
			{
				isInvoked = false;
				InvokeSchedule();
			}
		}
		else
		{
			controller.UpdatePerSec();
		}
	}

	public void UpdatePerSecNoRender()
	{
		if (isInvoked)
		{
			isInvoked = false;
		}
		controller.UpdatePerSec();
	}

	private bool _JourneyTo(string sceneName, Vector2 position)
	{
		if (sceneName.IsNullOrEmpty())
		{
			controller.ChangeTask(controller.JourneyTo(string.Empty, Vector2.zero));
			return true;
		}
		Vector2 remotePosition = ConstrainPositionHeight(sceneName, position);
		controller.ChangeTask(controller.JourneyTo(sceneName, remotePosition));
		return true;
	}

	public void Move(float delta)
	{
		if (delta != 0f)
		{
			positionWS = new Vector2(positionWS.x + delta, positionWS.y);
			if (IsRenderNow)
			{
				Renderer.mover.Move(delta, proto.WalkSpeed);
			}
		}
	}

	public void StopMove()
	{
		if (IsRenderNow)
		{
			Renderer.mover.StopMove();
		}
	}

	public bool __EnterScene(string sceneName, Vector2 remotePosition, bool shouldSnapToGround)
	{
		if (CompareSceneName(sceneName))
		{
			return true;
		}
		this.sceneName = sceneName;
		if (sceneName.StartsWith("city_") && DolocAPI.assets.cityRooms.QueryDataBySceneName(sceneName, out var roomProto))
		{
			Vector2Int position = roomProto.geometry.CalcCellPosition(remotePosition);
			if (shouldSnapToGround && roomProto.geometry.RaycastGround(position, out var groundPosition))
			{
				remotePosition.y = roomProto.geometry.CalcWorldPosition(groundPosition, new Vector2(0.5f, 0f)).y;
			}
			else
			{
				remotePosition.y = roomProto.GroundLine;
			}
		}
		positionWS = remotePosition;
		_SetRendererStatusFromCurrentScene();
		return true;
	}

	public void VisitNpcName()
	{
		if (!hasKnownName)
		{
			hasKnownName = true;
			DolocAPI.BroadcastString(GameEventType.VISIT_NPC_NAME, NpcName);
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Npc, NpcName);
		}
	}

	public void ClearNpcName()
	{
		hasKnownName = false;
	}

	public void SetAliasId(string aliasId)
	{
		this.aliasId = aliasId;
	}

	public void SetNpcWork(NpcScheduleWork work)
	{
		controller.SetScheduleWork(work);
	}

	public bool TryGetCurrentRoom(out Room room)
	{
		room = null;
		if (sceneName.IsNullOrEmpty())
		{
			return false;
		}
		if (sceneName.StartsWith("dungeon_"))
		{
			string sceneShortName = sceneName.Replace("dungeon_", string.Empty);
			Dungeon dungeon = DolocAPI.archiveHandle.dungeonData.dungeonManager.GetDungeon(sceneShortName);
			if (dungeon == null)
			{
				return false;
			}
			room = dungeon.GetRoomFromPosition(positionWS);
			if (room != null)
			{
				return true;
			}
		}
		return DolocAPI.QueryRoom(sceneName, out room);
	}

	public bool IsAtMarkPoint(string name, float threshold = 5f)
	{
		if (sceneName.IsNullOrEmpty())
		{
			return false;
		}
		MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(name);
		if (orDefault == null)
		{
			return false;
		}
		if (orDefault.SceneRawName == sceneName)
		{
			return Mathf.Abs(orDefault.Position.x - positionWS.x) < threshold;
		}
		return false;
	}

	public bool CompareSceneName(string sceneName)
	{
		if (string.IsNullOrEmpty(sceneName))
		{
			return IsAtVoidScene;
		}
		return this.sceneName == sceneName;
	}

	public void ManualSetToMarkPoint(string markPointId, bool shouldStreet = true)
	{
		MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(markPointId);
		if (orDefault != null)
		{
			ManualSetToMarkPoint(orDefault, shouldStreet);
		}
	}

	private void ManualSetToMarkPoint(MarkPointInfo markPoint, bool shouldStreet = true)
	{
		ManualSetPosition(markPoint.SceneRawName, markPoint.Position, shouldStreet);
	}

	public void ManualSetPosition(string sceneName, Vector2 position, bool shouldStreet = true)
	{
		positionWS = position;
		if (IsRenderNow)
		{
			Renderer.positionWS = position;
		}
		if (sceneName != this.sceneName)
		{
			this.sceneName = sceneName;
			_SetRendererStatusFromCurrentScene();
		}
		if (isMoving)
		{
			StopMove();
		}
		if (shouldStreet)
		{
			controller.ChangeTask(new NpcWorkTaskStreet());
		}
	}

	private Vector2 GetStreetPoint(string sceneName, bool overrideHeight = true)
	{
		if (string.IsNullOrEmpty(sceneName))
		{
			return Vector2.zero;
		}
		if (proto.TryGetRandomPositionInScene(sceneName, out var point))
		{
			return ConstrainPositionHeight(sceneName, point);
		}
		Debug.LogWarning("Npc\"" + NpcName + "\"在场景\"" + sceneName + "\"没有站街点");
		RoomProto roomProto;
		if (DolocAPI.sceneManager.GetSceneType(sceneName) == SceneType.CITY)
		{
			return DolocAPI.assets.cityRooms.QueryDataBySceneName(sceneName, out roomProto) ? roomProto.RandomStreetPoint : Vector2.zero;
		}
		return Vector2.zero;
	}

	private Vector2 ConstrainPositionHeight(string sceneName, Vector2 position)
	{
		if (DolocAPI.assets.cityRooms.QueryData(sceneName, out var roomProto) && Mathf.Abs(roomProto.GroundLine) > 0.1f)
		{
			position.y = roomProto.GroundLine;
		}
		return position;
	}

	public void ManualSetPosition(string sceneName, bool overrideHeight = true, bool shouldStreet = true)
	{
		Vector2 streetPoint = GetStreetPoint(sceneName, overrideHeight);
		ManualSetPosition(sceneName, streetPoint, shouldStreet);
	}

	public void ManualSetPosition(Vector2 position, bool shouldStreet = true)
	{
		ManualSetPosition(sceneName, position, shouldStreet);
	}

	public void ManualSetPositionToAgentScene(Vector2 position, bool shouldStreet = true)
	{
		string text = DolocAPI.archiveHandle.currentRoom?.SceneRawName ?? string.Empty;
		ManualSetPosition(text, position, shouldStreet);
	}

	public void EnableSchedule()
	{
		overrideMarkPoint = null;
		InvokeSchedule();
	}

	public void DisableSchedule(string markPointId)
	{
		overrideMarkPoint = markPointId;
		InvokeSchedule();
	}

	public void DebugSchedule(NpcScheduleGraph graph)
	{
		if (!(graph == null))
		{
			DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
			WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
			NpcScheduleParams status = new NpcScheduleParams(dateNow, currentWeatherType);
			if (!graph.QuerySchedule(status, out var result))
			{
				Debug.LogError("无法获取计划表" + graph.name + "结果");
			}
			else
			{
				SetNpcWork(result.work);
			}
		}
	}

	public void InvokeSchedule()
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		InvokeSchedule(dateNow, currentWeatherType);
	}

	public void InvokeSchedule(DateInfo dateInfo, WeatherType weatherType)
	{
		NpcScheduleResult scheduleResult;
		if (!overrideMarkPoint.IsNullOrEmpty())
		{
			string markPointName = (DolocConfig.Tables.TbMarkPoint.DataMap.ContainsKey(overrideMarkPoint ?? "") ? overrideMarkPoint : proto.InitialMarkPoint);
			scheduleResult = new NpcScheduleResult(markPointName, NpcScheduleWork.Street);
		}
		else
		{
			NpcScheduleParams status = new NpcScheduleParams(dateInfo, weatherType);
			scheduleResult = proto.QueryScheduleResult(status);
			controller.SetScheduleResult(scheduleResult);
		}
		if (IsAtMarkPoint(scheduleResult.markPointName))
		{
			return;
		}
		string text = scheduleResult.sceneName;
		_currentJourneyInfo = (string.IsNullOrEmpty(text) ? "虚空场景" : ("【" + scheduleResult.markPointName + "】" + scheduleResult.sceneName));
		if (!SceneUtils.IsSceneExist(text))
		{
			if (!string.IsNullOrEmpty(text))
			{
				Debug.LogWarning("Npc\"" + proto.Id + "\"计划表中的目标场景\"" + text + "\"不存在，将Npc放置到虚空场景");
			}
			_JourneyTo(string.Empty, Vector2.zero);
		}
		else
		{
			_JourneyTo(text, scheduleResult.position);
		}
	}

	public void RefreshNpcEventStatus()
	{
		if (IsRenderNow)
		{
			Renderer.SetEventFlagState(HasEventNow);
		}
	}
}
