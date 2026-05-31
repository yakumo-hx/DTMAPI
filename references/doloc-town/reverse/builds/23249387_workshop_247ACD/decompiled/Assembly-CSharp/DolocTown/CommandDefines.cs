using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Room;
using DolocTown.GameData;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class CommandDefines
{
	private enum SubmitItemType
	{
		Plant,
		Chip
	}

	private enum SubmitCountType
	{
		None,
		Single,
		Multiple,
		Overflow
	}

	private static Vector2 backupCamPos = Vector2.zero;

	private static Vector2 backupPlayerPos;

	private static string backupString;

	private static float backupNumber;

	private static bool backupBool;

	[Command("archive_open_plant_submit_panel", Desc = "【档案馆】打开植物提交界面")]
	private static UniTask Command_OpenPlantSubmitPanel()
	{
		DolocAPI.EnterUI<PlantSubmitUiState>();
		return DolocAPI.WaitWhileInUiSateTask();
	}

	[Command("archive_submit_plant_status", Desc = "【档案馆】上次提交植物的情况")]
	private static string Command_GetLastSubmitPlantStatus()
	{
		return CheckArchiveSubmitStatus(SubmitItemType.Plant).ToString().ToLower();
	}

	[Command("archive_analyze_plant", Desc = "【档案馆】开始分析植物")]
	private static void Command_AnalyzePlant()
	{
		DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.AnalyzePlant();
	}

	[Command("is_all_plant_docs_unlocked", Desc = "【档案馆】已解锁所有植物档案")]
	private static bool Command_IsAllPlantDocUnlocked()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.allPlantDocUnlocked;
	}

	[Command("unlock_plant_doc", Desc = "【档案馆】解锁指定的植物档案")]
	private static void Command_UnlockPlantDoc(string plantName)
	{
		DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.UnlockPlantDocument(plantName);
	}

	[Command("get_unlocked_plant_doc_count", Desc = "【档案馆】获取解锁的植物档案数")]
	private static int Command_GetUnlockedPlantDocCount()
	{
		return DolocAPI.archiveHandle.GetUnlockedPlantDocumentCount();
	}

	[Command("get_latest_plant_title", Desc = "【档案馆】获取最近一次分析的植物名串")]
	private static string Command_GetLatestPlantTitles()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.GetLatestPlantTitles();
	}

	[Command("get_latest_plant_reward", Desc = "【档案馆】获取最近一次分析的报酬")]
	private static int Command_GetLatestPlantReward()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.GetLatestPlantReward();
	}

	[Command("get_latest_plant_count", Desc = "【档案馆】获取最近一次分析的数量")]
	private static int Command_GetLatestPlantCount()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.GetLatestPlantCount();
	}

	[Command("archive_open_chip_submit_panel", Desc = "【档案馆】打开芯片提交界面")]
	private static UniTask Command_OpenChipSubmitPanel()
	{
		DolocAPI.EnterUI<ChipSubmitUiState>();
		return DolocAPI.WaitWhileInUiSateTask();
	}

	[Command("is_all_chip_docs_unlocked", Desc = "【档案馆】已解锁所有芯片档案")]
	private static bool Command_IsAllChipDocUnlocked()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.allChipDocUnlocked;
	}

	[Command("archive_analyze_chip", Desc = "【档案馆】开始分析芯片")]
	private static void Command_AnalyzeChip()
	{
		DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.AnalyzeChips();
	}

	[Command("archive_submit_chip_status", Desc = "【档案馆】上次提交芯片的情况")]
	private static string Command_GetLastSubmitChipStatus()
	{
		return CheckArchiveSubmitStatus(SubmitItemType.Chip).ToString().ToLower();
	}

	[Command("is_chip_analyze_complete", Desc = "【档案馆】芯片分析是否结束")]
	private static bool Command_IsChipAnalyzeComplete()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.ChipRemainingAnalysisHour() <= 0;
	}

	[Command("get_chip_analyze_remain_hours", Desc = "【档案馆】芯片分析剩余的小时数")]
	private static string Command_GetChipAnalyzeRemainHours()
	{
		return DolocAPI.GetFormatTimeLengthByHours(DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.ChipRemainingAnalysisHour());
	}

	[Command("get_latest_chip_doc_title", Desc = "【档案馆】上次解锁的档案标题串")]
	private static string Command_GetLatestChipDocTitle()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.GetLatestChipDocTitle();
	}

	[Command("get_latest_chip_doc_count", Desc = "【档案馆】上次解锁的档案数")]
	private static int Command_GetLatestChipDocCount()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.GetLatestChipDocCount();
	}

	[Command("get_chip_submit_limit", Desc = "【档案馆】提交芯片的上限")]
	private static int Command_GetChipSubmitLimit()
	{
		return DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.chipSubmitLimit;
	}

	[Command("archive_submit_first_chip", Desc = "【档案馆】提交一个芯片到档案馆(用于初始任务)")]
	private static void Command_SubmitFirstChip()
	{
		DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr.SubmitFirstChip();
	}

	private static SubmitCountType CheckArchiveSubmitStatus(SubmitItemType submitItemType)
	{
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		if (documentManager.latestSubmitItems.IsNullOrEmpty())
		{
			return SubmitCountType.None;
		}
		int num = documentManager.latestSubmitItems.Sum((Item x) => x.count);
		if (documentManager.latestOverflowItems.Length >= 1)
		{
			return SubmitCountType.Overflow;
		}
		if (documentManager.latestSubmitItems.Length == 1 && num == 1 && documentManager.latestOverflowItems.IsNullOrEmpty())
		{
			return SubmitCountType.Single;
		}
		if ((documentManager.latestSubmitItems.Length > 1 || num > 1) && documentManager.latestOverflowItems.IsNullOrEmpty())
		{
			return SubmitCountType.Multiple;
		}
		return SubmitCountType.None;
	}

	[Command("face_left", Desc = "使对象朝向左边")]
	private static void FaceLeft(string entityName)
	{
		DolocAPI.GetDialogueTargetViewOrDefault(entityName).FaceLeft();
	}

	[Command("face_right", Desc = "使对象朝向右边")]
	private static void FaceRight(string entityName)
	{
		DolocAPI.GetDialogueTargetViewOrDefault(entityName).FaceLeft(value: false);
	}

	[Command("face_position", Desc = "使对象朝向某x坐标")]
	private static void FacePosition(string entityName, float x)
	{
		DolocAPI.GetDialogueTargetViewOrDefault(entityName).LookAt(x);
	}

	[Command("face_target", Desc = "使某对象朝向另一个目标对象")]
	private static void FaceTarget(string entityName, string targetEntityName, bool mutual = false)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(entityName);
		IDialogueEntity dialogueTargetViewOrDefault2 = DolocAPI.GetDialogueTargetViewOrDefault(targetEntityName);
		dialogueTargetViewOrDefault.LookAt(dialogueTargetViewOrDefault2.WorldPosition.x);
		if (mutual)
		{
			dialogueTargetViewOrDefault2.LookAt(dialogueTargetViewOrDefault.WorldPosition.x);
		}
	}

	[Command("set_pos", Desc = "将指定对象放在当前场景的某位置(会将npc强制移动到当前场景)")]
	private static void SetTargetPos(string entityName, float x, float y)
	{
		DolocAPI.GetDialogueTargetViewOrDefault(entityName, force: true).SetWorldPosition(new Vector2(x, y));
	}

	[Command("set_to_mark", Desc = "将指定对象放在某标记点")]
	private static void SetTargetToMark(string npc, string mark)
	{
		DolocAPI.GetDialogueTargetViewOrDefault(npc, force: true).SetToMarkPoint(mark);
	}

	[Command("set_to_current_interactable", Desc = "将指定对象放在当前交互的交互物位置")]
	private static void SetTargetToCurrentInteractable(string npc)
	{
		if (!(DolocAPI.CurrentInteractableObject == null))
		{
			Vector3 position = DolocAPI.CurrentInteractableObject.position;
			DolocAPI.GetDialogueTargetViewOrDefault(npc, force: true).SetWorldPosition(position);
		}
	}

	[Command("hide_current_interactable", Desc = "隐藏当前的交互物")]
	private static void HideCurrentInteractable()
	{
		if (!(DolocAPI.CurrentInteractableObject == null))
		{
			DolocAPI.CurrentInteractableObject.gameObject.SetActive(value: false);
		}
	}

	[Command("interact_with", Desc = "触发一次与指定npc的交互")]
	private static void InteractWith(string npc)
	{
		if (DolocAPI.QueryNpc(npc, out var npc2))
		{
			npc2.ForceInteract();
		}
	}

	[Command("set_to_target", Desc = "将指定对象放在目标对象的位置，并进行偏移")]
	private static void SetToTarget(string src, string dst, float offsetX = 0f, float offsetY = 0f)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(src, force: true);
		IDialogueEntity dialogueTargetViewOrDefault2 = DolocAPI.GetDialogueTargetViewOrDefault(dst, force: true);
		dialogueTargetViewOrDefault.SetWorldPosition(dialogueTargetViewOrDefault2.WorldPosition + new Vector2(offsetX, offsetY));
	}

	[Command("set_order", Desc = "设置对象排序层级(动画中npc/玩家为2，杂草为1，仅对在场的对象有效，对话结束后层级会恢复)")]
	private static void SetToTarget(string npcName, int order)
	{
		DolocAPI.GetDialogueTargetViewOrDefault(npcName, force: true).SetSortingOrder("GroundBack", order);
	}

	[Command("show_emotion", Desc = "使对象弹出表情")]
	private static async UniTask ShowEmotion(string entityName, string emotionName, int duration = 2)
	{
		DolocAPI.GetDialogueTargetViewOrDefault(entityName).ShowEmotion(emotionName);
		await UniTask.Delay(TimeSpan.FromSeconds(duration));
	}

	[Command("raise_continues_ps", Desc = "在对象位置生成一个持续特效")]
	private static async UniTask RaiseContinuesPS(string entityName, string effType, int duration = 2, float offsetX = 0f, float offsetY = 0f)
	{
		Vector2 worldPosition = DolocAPI.GetDialogueTargetViewOrDefault(entityName).WorldPosition;
		worldPosition.x += offsetX;
		worldPosition.y += offsetY;
		if (Enum.TryParse<ContinuesParticleEffectsType>(effType, ignoreCase: true, out var result))
		{
			ContinuesParticleEffects effects = DolocAPI.RaiseContinuesPS(worldPosition, result);
			await UniTask.Delay(TimeSpan.FromSeconds(duration));
			effects.Stop();
		}
	}

	[Command("raise_continues_ps_at_pos", Desc = "在指定位置生成一个持续特效")]
	private static async UniTask RaiseContinuesPS(float x, float y, string effType, int duration = 2)
	{
		Vector2 positionWS = new Vector2(x, y);
		if (Enum.TryParse<ContinuesParticleEffectsType>(effType, ignoreCase: true, out var result))
		{
			ContinuesParticleEffects effects = DolocAPI.RaiseContinuesPS(positionWS, result);
			await UniTask.Delay(TimeSpan.FromSeconds(duration));
			effects.Stop();
		}
	}

	[Command("walk_to", Desc = "使对象行走到指定位置")]
	private static async UniTask WalkTo(string entityName, float x, float y, float speed = 0f)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(entityName);
		if (speed <= 0f)
		{
			speed = dialogueTargetViewOrDefault.DefaultWalkSpeed;
		}
		await dialogueTargetViewOrDefault.WalkTo(new Vector2(x, y), speed);
	}

	[Command("walk_to_mark", Desc = "使对象行走到指定标记点的坐标(不会切换场景)")]
	private static async UniTask WalkToMark(string entityName, string mark, float speed = 0f, float offsetX = 0f, float offsetY = 0f)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(entityName);
		Vector2 pos = (DolocConfig.Tables.TbMarkPoint.GetOrDefault(mark)?.Position ?? Vector2.zero) + new Vector2(offsetX, offsetY);
		if (speed <= 0f)
		{
			speed = dialogueTargetViewOrDefault.DefaultWalkSpeed;
		}
		await dialogueTargetViewOrDefault.WalkTo(pos, speed);
	}

	[Command("walk_to_mark_global", Desc = "使对象行走到指定标记点的坐标(不会切换场景，若标记点处于不同场景，则会朝着地图上的该方向走)")]
	private static async UniTask WalkToMarkGlobal(string entityName, string mark, float walkDistance = 100f, float speed = 0f)
	{
		MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(mark);
		DolocAPI.QueryRoom(orDefault?.RoomId, out var room);
		Vector2 pos;
		Vector2 mapPosition3;
		if (orDefault != null && DolocAPI.QueryNpc(entityName, out var npc))
		{
			npc.TryGetCurrentRoom(out var room2);
			if (npc.sceneName != orDefault.SceneRawName && DolocAPI.GetMapPosByWorldPosition(room2, npc.positionWS, out var mapPosition) && DolocAPI.GetMapPosByWorldPosition(room, orDefault.Position, out var mapPosition2))
			{
				if (mapPosition.x > mapPosition2.x)
				{
					await WalkLeft(entityName, walkDistance, speed);
				}
				else
				{
					await WalkRight(entityName, walkDistance, speed);
				}
				return;
			}
		}
		else if (orDefault != null && entityName == "player" && DolocAPI.archiveHandle.currentSceneName != orDefault.SceneRawName && DolocAPI.GetAgentPosInMap(out pos) && DolocAPI.GetMapPosByWorldPosition(room, orDefault.Position, out mapPosition3))
		{
			if (pos.x > mapPosition3.x)
			{
				await WalkLeft(entityName, walkDistance, speed);
			}
			else
			{
				await WalkRight(entityName, walkDistance, speed);
			}
			return;
		}
		await WalkToMark(entityName, mark, speed);
	}

	[Command("walk_left", Desc = "使对象朝左行走一段距离")]
	private static async UniTask WalkLeft(string entityName, float walkDistance = 100f, float speed = 0f)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(entityName);
		if (speed <= 0f)
		{
			speed = dialogueTargetViewOrDefault.DefaultWalkSpeed;
		}
		Vector2 pos = dialogueTargetViewOrDefault.WorldPosition - new Vector2(walkDistance, 0f);
		await dialogueTargetViewOrDefault.WalkTo(pos, speed);
	}

	[Command("walk_right", Desc = "使对象朝右行走一段距离")]
	private static async UniTask WalkRight(string entityName, float walkDistance = 100f, float speed = 0f)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(entityName);
		if (speed <= 0f)
		{
			speed = dialogueTargetViewOrDefault.DefaultWalkSpeed;
		}
		Vector2 pos = dialogueTargetViewOrDefault.WorldPosition + new Vector2(walkDistance, 0f);
		await dialogueTargetViewOrDefault.WalkTo(pos, speed);
	}

	[Command("walk_to_target", Desc = "使对象行走到目标对象位置(带偏移)")]
	private static async UniTask WalkToTarget(string src, string dst, float speed = 0f, float offsetX = 0f, float offsetY = 0f)
	{
		IDialogueEntity dialogueTargetViewOrDefault = DolocAPI.GetDialogueTargetViewOrDefault(src, force: true);
		Vector2 pos = DolocAPI.GetDialogueTargetViewOrDefault(dst, force: true).WorldPosition + new Vector2(offsetX, offsetY);
		if (speed <= 0f)
		{
			speed = dialogueTargetViewOrDefault.DefaultWalkSpeed;
		}
		await dialogueTargetViewOrDefault.WalkTo(pos, speed);
	}

	[Command("play_anim", Desc = "播放对象的动画")]
	private static async UniTask PlayNpcAnimation(string entityName, string animName, bool force = false)
	{
		await DolocAPI.GetDialogueTargetViewOrDefault(entityName).PlayAnimationAsync(animName, force);
	}

	[Command("disable_next_fade_in", Desc = "禁用下次淡入")]
	private static void DisableNextFadeIn(float duration = 0.3f)
	{
		DolocAPI.ppm.DisableFadeInOnce = true;
	}

	[Command("disable_next_fade_out", Desc = "禁用下次淡出")]
	private static void DisableNextFadeOut(float duration = 0.3f)
	{
		DolocAPI.ppm.DisableFadeOutOnce = true;
	}

	[Command("fade_in", Desc = "淡入")]
	private static async UniTask FadeIn(float duration = 0.3f)
	{
		DolocAPI.ppm.FadeIn(duration);
		await UniTask.Delay((int)(duration * 1000f));
	}

	[Command("fade_out", Desc = "淡出")]
	private static async UniTask FadeOut(float duration = 0.3f)
	{
		DolocAPI.ppm.FadeOut(duration, null, shouldReset: false, "FadeOut");
		await UniTask.Delay((int)(duration * 1000f));
	}

	[Command("enter_cinema_screen", Desc = "进入电影状态")]
	private static async UniTask EnterCinemaScreen(float duration = 0.3f)
	{
		DolocAPI.gameStateManager.dialogState.EnableCinemaScreen();
		await UniTask.Delay((int)(duration * 1000f));
	}

	[Command("exit_cinema_screen", Desc = "退出电影状态")]
	private static async UniTask ExitCinemaScreen(float duration = 0.3f)
	{
		DolocAPI.gameStateManager.dialogState.IgnoreCinemaScreenOnResumeOnce = true;
		DolocAPI.SetPPM_CinemaScreen(value: false);
		await UniTask.Delay((int)(duration * 1000f));
	}

	[Command("shake_screen", Desc = "震屏效果")]
	private static async UniTask ShakeScreen(float shakeDuration = 0.2f, float shakeStrength = 0.3f)
	{
		DolocAPI.cameraController.ShakeScreen(shakeDuration, shakeStrength);
		await UniTask.Delay((int)(shakeDuration * 1000f));
	}

	[Command("backup_cam", Desc = "备份相机位置")]
	private static void BackupCam()
	{
		backupCamPos = DolocAPI.cameraController.position2d;
	}

	[Command("revert_cam", Desc = "恢复相机到上次备份的位置")]
	private static async UniTask RevertCam(float duration = 1f, string ease = "linear")
	{
		await MoveCam(backupCamPos.x, backupCamPos.y, duration, ease);
		DolocAPI.cameraController.setEnabled(value: true);
	}

	[Command("backup_player_pos", Desc = "备份玩家位置，与revert_player_pos配合使用")]
	private static void BackupPlayerPos()
	{
		backupPlayerPos = DolocAPI.AgentPosition;
	}

	[Command("revert_player_pos", Desc = "恢复玩家到上次备份的位置")]
	private static void RevertPlayerPos()
	{
		DolocAPI.AgentPosition = backupPlayerPos;
	}

	[Command("backup_string", Desc = "备份字符串")]
	public static void BackupString(string str)
	{
		backupString = str;
	}

	[Command("get_backup_string", Desc = "获取备份的字符串")]
	private static string GetBackupString()
	{
		return backupString;
	}

	[Command("backup_number", Desc = "备份浮点数")]
	public static void BackupNumber(float number)
	{
		backupNumber = number;
	}

	[Command("get_backup_number", Desc = "获取备份的字符串")]
	private static float GetBackupNumber()
	{
		return backupNumber;
	}

	[Command("backup_bool", Desc = "备份布尔值")]
	public static void BackupBool(bool value)
	{
		backupBool = value;
	}

	[Command("get_backup_bool", Desc = "获取备份的布尔值")]
	private static bool GetBackupBool()
	{
		return backupBool;
	}

	[Command("move_cam", Desc = "移动相机")]
	private static async UniTask MoveCam(float x, float y, float duration = 1f, string ease = "linear")
	{
		if (duration <= 0f)
		{
			SetCamPos(x, y);
			return;
		}
		Enum.TryParse(typeof(Ease), ease, ignoreCase: true, out var result);
		bool wait = true;
		DolocAPI.cameraController.MoveToByDuration(new Vector2(x, y), duration, (Ease)result, delegate
		{
			wait = false;
		});
		await UniTask.WaitUntil(() => !wait);
	}

	[Command("move_cam_to_mark", Desc = "移动相机到标记点")]
	private static async UniTask MoveCam(string mark, float duration = 1f, string ease = "linear")
	{
		Vector2 vector = DolocConfig.Tables.TbMarkPoint.GetOrDefault(mark)?.Position ?? Vector2.zero;
		await MoveCam(vector.x, vector.y, duration, ease);
	}

	[Command("move_cam_to_mark_x", Desc = "在x轴上移动相机到标记点")]
	private static async UniTask MoveCamX(string mark, float duration = 1f, string ease = "linear")
	{
		Vector2 obj = DolocConfig.Tables.TbMarkPoint.GetOrDefault(mark)?.Position ?? Vector2.zero;
		await MoveCam(obj.x, DolocAPI.cameraController.position2d.y, duration, ease);
	}

	[Command("move_cam_to_mark_y", Desc = "在y轴上移动相机到标记点")]
	private static async UniTask MoveCamY(string mark, float duration = 1f, string ease = "linear")
	{
		Vector2 vector = DolocConfig.Tables.TbMarkPoint.GetOrDefault(mark)?.Position ?? Vector2.zero;
		await MoveCam(DolocAPI.cameraController.position2d.x, vector.y, duration, ease);
	}

	[Command("set_cam_pos", Desc = "设置相机位置")]
	private static void SetCamPos(float x, float y)
	{
		DolocAPI.cameraController.ForceSetPosition(new Vector2(x, y));
	}

	[Command("post_sound_event", Desc = "触发音频事件")]
	private static void PostSoundEvent(string soundEventType)
	{
		DolocAPI.Sound.PostSoundEvent(soundEventType);
	}

	[Command("play_bgm", Desc = "播放bmg, 不指定则播放当前默认")]
	private static void PostBGMEvent(string soundEvent = null)
	{
		DolocAPI.Sound.PlayBgm(soundEvent);
	}

	[Command("refresh_bgm_later", Desc = "当前播放完成后播放默认bgm")]
	private static void RefreshBgmLater()
	{
		DolocAPI.Sound.RefreshBgmLater();
	}

	[Command("stop_bgm", Desc = "停止bmg")]
	private static void StopBGM()
	{
		DolocAPI.Sound.StopBgm();
	}

	[Command("play_ambience", Desc = "播放环境音, 不指定则播放当前默认")]
	private static void PostAmbienceEvent(string soundEvent = null)
	{
		if (soundEvent.IsNullOrEmpty())
		{
			DolocAPI.Sound.RefreshAmbience();
		}
		else
		{
			DolocAPI.Sound.PlayAmbience(soundEvent);
		}
	}

	[Command("stop_ambience", Desc = "停止环境音")]
	private static void StopAmbience()
	{
		DolocAPI.Sound.StopAmbience();
	}

	[Command("play_melody", Desc = "播放旋律")]
	private static async UniTask PlayMelody(string notes, float interval = 0.12f)
	{
		if (notes.IsNullOrEmpty() || notes.Length == 0)
		{
			return;
		}
		string eventFormat = "Play_Character_Dial_0{0}";
		foreach (char c in notes)
		{
			if (DolocAPI.Sound.PostSoundEvent(eventFormat.Format(c)) && notes.Length > 1)
			{
				await UniTask.Delay((int)(interval * 1000f));
			}
		}
	}

	[Command("start_festival", Desc = "进入节日")]
	private static void StartFestival(string festivalName)
	{
		DolocAPI.WaitToEnterFestivalState(festivalName);
	}

	[Command("stop_festival", Desc = "退出节日")]
	private static void StopFestival()
	{
		DolocAPI.WaitToExitFestivalState();
	}

	[Command("init_npc_in_festival", Desc = "初始化npc的节日状态")]
	private static void InitNpcInFestival(string npcName)
	{
		DolocAPI.InitNpcInFestival(npcName);
	}

	[Command("add_dialogue", Desc = "添加对话节点到目标单位，npcName默认为脚本tag中配置的@npc_name")]
	private static void AddDialogueNode(string nodeName, string npcName = null)
	{
		DolocAPI.AddDialogueNode(nodeName, npcName);
	}

	[Command("start_dialogue", Desc = "(当前对话结束后)执行对话节点，npcName默认为脚本tag中配置的@npc_name")]
	private static void StartDialogueNode(string nodeName, string npcName = null)
	{
		DolocAPI.StartDialogueNode(nodeName, npcName);
	}

	[Command("set_dialogue_entrance", Desc = "设置目标单位的入口对话节点")]
	private static void SetDialogueEntrance(string nodeName, string npcName = null)
	{
		DolocAPI.SetDialogueEntrance(nodeName, npcName);
	}

	[Command("remove_dialogue", Desc = "删除对话节点")]
	private static void RemoveDialogue(string nodeName, string npcName = null)
	{
		DolocAPI.RemoveDialogueNode(nodeName, npcName);
	}

	[Command("visit_npc_name", Desc = "显示npc名称")]
	private static void VisitNpcName(string npcName)
	{
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			npc.VisitNpcName();
		}
	}

	[Command("set_npc_alias", Desc = "设置npc的别名")]
	private static void SetNpcAlias(string npcName, string aliasId)
	{
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			npc.SetAliasId(aliasId);
			npc.VisitNpcName();
		}
	}

	[Command("clear_npc_alias", Desc = "清除npc的别名")]
	private static void ClearNpcAlias(string npcName)
	{
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			npc.SetAliasId(string.Empty);
			npc.ClearNpcName();
		}
	}

	[Command("send_complete_event", Desc = "发送对话完成消息，默认参数为当前节点id")]
	private static void SendCompleteEvent(string nodeName = null)
	{
		if (string.IsNullOrEmpty(nodeName))
		{
			nodeName = DolocAPI.archiveHandle.cityData.dialogueManager.CurrentNode;
		}
		string defaultNpcName = DolocAPI.archiveHandle.cityData.dialogueManager.DefaultNpcName;
		DolocAPI.BroadcastString(GameEventType.COMPLETE_DIALOGUE, nodeName);
		DolocAPI.RemoveDialogueNode(nodeName, defaultNpcName);
		Debug.Log("完成对话：" + nodeName);
	}

	[Command("send_item_as_email", Desc = "发送道具到邮箱")]
	private static void SendItemAsEmail(string itemName, int count)
	{
		DolocAPI.SendItemAsEmail(itemName, count);
	}

	[Command("wait", Desc = "")]
	private static async UniTask WaitForSeconds(float duration)
	{
		await UniTask.Delay((int)(duration * 1000f));
	}

	[Command("return_to_candidate_options", Desc = "结束后回到对话选择列表")]
	private static void TracebackDialogue()
	{
		DolocAPI.gameStateManager.dialogState.ReturnToCandidates();
	}

	[Command("handle_idle_talk", Desc = "随机进行一次闲聊")]
	private static void HandleIdleTalk()
	{
		DolocAPI.gameStateManager.dialogState.HandleIdleTalk();
	}

	[Command("quit", Desc = "退出对话状态")]
	private static void QuitDialogue()
	{
		DolocAPI.DelayFrame(delegate
		{
			DolocAPI.gameStateManager.dialogState.QuitDialogue();
		});
	}

	[Command("ignore_option_visit", Desc = "下个选项组不记录是否选择过状态（选过后不会置灰）")]
	private static void DialogueIgnoreNextOptionVisit()
	{
		DolocAPI.gameStateManager.dialogState.IgnoreNextOptionVisit();
	}

	[Command("visit", Desc = "增加一次指定id的访问次数")]
	private static void Visit(string id)
	{
		if (!id.IsNullOrEmpty())
		{
			DolocAPI.archiveHandle.cityData.dialogueManager.Visit(id);
		}
	}

	[Command("is_visited", Desc = "是否访问过指定id")]
	private static bool HasVisited(string id)
	{
		return DolocAPI.archiveHandle.cityData.dialogueManager.HasVisited(id);
	}

	[Command("get_visited_count", Desc = "获取指定id的访问次数")]
	private static int GetVisitedCount(string id)
	{
		return DolocAPI.archiveHandle.cityData.dialogueManager.GetVisitedCount(id);
	}

	[Command("clear_visited", Desc = "清零指定id的访问次数")]
	private static void ClearVisitedCount(string id)
	{
		DolocAPI.archiveHandle.cityData.dialogueManager.ClearVisitedCount(id);
	}

	[Command("mark_time", Desc = "记录时间戳(需要指定id)")]
	private static void MarkTimeStamp(string id)
	{
		DolocAPI.archiveHandle.cityData.dialogueManager.MarkTimeStamp(id, DolocAPI.archiveHandle.DateNow);
	}

	[Command("get_minutes_since", Desc = "从指定时间戳到现在过去了多少分钟，没记录过的话返回-1")]
	private static int GetMinutesSince(string id)
	{
		if (!DolocAPI.archiveHandle.cityData.dialogueManager.GetTimeStamp(id, out var dateInfo))
		{
			return -1;
		}
		return DolocAPI.archiveHandle.DateNow.CalMinuteDiff(dateInfo);
	}

	[Command("get_hours_since", Desc = "从指定时间戳到现在过去了多少小时，没记录过的话返回-1")]
	private static int GetHoursSince(string id)
	{
		if (!DolocAPI.archiveHandle.cityData.dialogueManager.GetTimeStamp(id, out var dateInfo))
		{
			return -1;
		}
		return DolocAPI.archiveHandle.DateNow.CalHourDiff(dateInfo);
	}

	[Command("get_days_since", Desc = "从指定时间戳到现在过去了多少天，没记录过的话返回-1")]
	private static int GetDaysSince(string id)
	{
		if (!DolocAPI.archiveHandle.cityData.dialogueManager.GetTimeStamp(id, out var _))
		{
			return -1;
		}
		return GetHoursSince(id) / DolocAPI.GlobalParameter.Day2Hour;
	}

	[Command("lock_dash", Desc = "锁定冲刺")]
	private static void LockDash()
	{
		DolocAPI.archiveHandle.LockDash();
	}

	[Command("unlock_dash", Desc = "解锁冲刺")]
	private static void UnlockDash()
	{
		DolocAPI.archiveHandle.UnlockDash();
		DolocAPI.ShowMessageBoxLarge(LocSprites.UI_MENUICON_GIFT, DolocConfig.StaticTexts.AbilitySprintUnlockTip);
	}

	[Command("lock_double_jump", Desc = "锁定二段跳")]
	private static void LockDoubleJump()
	{
		DolocAPI.archiveHandle.LockDoubleJump();
	}

	[Command("unlock_double_jump", Desc = "解锁二段跳")]
	private static void UnlockDoubleJump()
	{
		DolocAPI.archiveHandle.UnlockDoubleJump();
		DolocAPI.ShowMessageBoxLarge(LocSprites.UI_MENUICON_GIFT, DolocConfig.StaticTexts.AbilityDoubleJumpUnlockTip);
		DolocAPI.agent.StateManager.GetState<AgentStateJump>().jumpTimes = DolocAPI.archiveHandle.DoubleJumpTimes();
	}

	[Command("unlock_building_link_gate", Desc = "解锁农场建筑联通门")]
	private static void UnlockBuildingLinkGate()
	{
		if (DolocAPI.archiveHandle.IsBuildingLinkGateUnlocked())
		{
			Debug.Log("已经解锁了农场建筑联通门");
			return;
		}
		DolocAPI.archiveHandle.UnlockBuildingLinkGate();
		Debug.Log("联通门已解锁");
	}

	[Command("is_calendar_unlocked", Desc = "是否解锁了日历功能")]
	private static bool IsCalendarUnlocked()
	{
		return DolocAPI.archiveHandle.IsCalendarUnlocked();
	}

	[Command("unlock_calendar", Desc = "解锁日历功能")]
	private static void UnlockCalendar()
	{
		DolocAPI.archiveHandle.UnlockCalendar();
	}

	[Command("unlock_additional_passive_slot", Desc = "解锁额外的被动道具格")]
	private static void UnlockAdditionalPassiveSlot()
	{
		DolocAPI.archiveHandle.UnlockAdditionalPassiveSlot();
	}
}
