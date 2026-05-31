using DolocTown.GameData;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class NpcController : DecisionMaker
{
	private readonly Npc _npc;

	private NpcScheduleResult _scheduleResult;

	public (string, Vector2) QuitPoint
	{
		get
		{
			if (_npc == null || _npc.proto.QuitMarkPoint.IsNullOrEmpty())
			{
				return (DolocAPI.gameConfig.VoidSceneName, DolocAPI.gameConfig.VoidScenePosition);
			}
			return (_npc.proto.QuitMarkPoint_Ref.SceneRawName, _npc.proto.QuitMarkPoint_Ref.Position);
		}
	}

	public string CurrentAnimationName
	{
		get
		{
			LinearTask currentTask = base.CurrentTask;
			if (!(currentTask is NpcTaskMove))
			{
				if (currentTask is NpcWorkTaskRelax)
				{
					return "relax";
				}
				return "idle";
			}
			return "walk";
		}
	}

	private LinearTask DefaultTask => WrapTask(new NpcWorkTaskStreet());

	private static LinearTask GetTaskFromPathActions(PathAction[] actions)
	{
		LinearTask linearTask = new LinearTaskWaitInt(1);
		foreach (PathAction pathAction in actions)
		{
			if (!(pathAction is PathActionMove pathActionMove))
			{
				if (pathAction is PathActionTeleport pathActionTeleport)
				{
					linearTask = linearTask.NpcEnterScene(pathActionTeleport.TargetSceneName, pathActionTeleport.TargetScenePosition);
				}
			}
			else
			{
				linearTask = linearTask.NpcMove(pathActionMove.Dest.x);
			}
		}
		return linearTask;
	}

	private static LinearTask GetTaskFromPathActions(LinearTask task, PathAction[] actions)
	{
		foreach (PathAction pathAction in actions)
		{
			if (!(pathAction is PathActionMove pathActionMove))
			{
				if (pathAction is PathActionTeleport pathActionTeleport)
				{
					task = task.NpcEnterScene(pathActionTeleport.TargetSceneName, pathActionTeleport.TargetScenePosition);
				}
			}
			else
			{
				task = task.NpcMove(pathActionMove.Dest.x);
			}
		}
		return task;
	}

	public NpcController(Npc npc)
	{
		_npc = npc;
	}

	public void SetScheduleResult(NpcScheduleResult result)
	{
		_scheduleResult = result;
	}

	public void SetScheduleWork(NpcScheduleWork work)
	{
		if (work != null)
		{
			_scheduleResult.work = work;
			RefreshWork();
		}
	}

	public LinearTask WrapTask(LinearTask task, bool fromHead = true)
	{
		if (fromHead)
		{
			task = task.Root;
		}
		task.ForEach(delegate(LinearTask t)
		{
			if (t is NpcTask npcTask)
			{
				npcTask.SetNpc(_npc);
			}
		});
		return task.Root;
	}

	public LinearTask JourneyTo(string sceneName, Vector2 remotePosition)
	{
		if (!string.IsNullOrEmpty(sceneName))
		{
			return _JourneyTo(sceneName, remotePosition);
		}
		return _JourneyToVoid();
	}

	private LinearTask _JourneyTo(string sceneName, Vector2 remotePosition)
	{
		if (_npc.IsAtVoidScene)
		{
			return _JourneyFromVoid(sceneName, remotePosition);
		}
		if (_npc.sceneName == sceneName && Mathf.Abs(remotePosition.y - _npc.positionWS.y) < 0.1f)
		{
			return WrapTask(new NpcTaskMove(remotePosition.x));
		}
		if (_npc.sceneName.StartsWith("dungeon_") || sceneName.StartsWith("dungeon_"))
		{
			return WrapTask(new NpcTaskFlash(sceneName, remotePosition));
		}
		PathAction[] array = DolocAPI.assets.cityRooms.GeneratePathActionsEx(_npc.sceneName, sceneName, _npc.positionWS, remotePosition);
		if (array != null)
		{
			return WrapTask(GetTaskFromPathActions(array));
		}
		return WrapTask(new NpcTaskFlash(sceneName, remotePosition));
	}

	private LinearTask _JourneyFromVoid(string targetSceneName, Vector2 targetPosition)
	{
		(string, Vector2) quitPoint = QuitPoint;
		if (!DolocAPI.assets.cityRooms.QueryDataBySceneName(quitPoint.Item1, out var voidScene))
		{
			return WrapTask(new NpcTaskFlash(targetSceneName));
		}
		PathAction[] array = DolocAPI.assets.cityRooms.GeneratePathActionsEx(voidScene.sceneInfo.name, targetSceneName, quitPoint.Item2, targetPosition);
		if (array == null)
		{
			Debug.LogWarning("找不到从虚空场景\"" + voidScene.name + "\"前往\"" + targetSceneName + "\"的路径");
			return WrapTask(new NpcTaskFlash(targetSceneName, targetPosition));
		}
		LinearTask taskFromPathActions = GetTaskFromPathActions(new NpcTaskAction(delegate(Npc npc)
		{
			npc.__EnterScene(voidScene.sceneInfo.name, quitPoint.Item2, shouldSnapToGround: false);
		}), array);
		return WrapTask(taskFromPathActions);
	}

	private LinearTask _JourneyToVoid()
	{
		if (_npc.IsAtVoidScene)
		{
			return LinearTask.WaitSeconds(1f);
		}
		(string, Vector2) quitPoint = QuitPoint;
		if (!DolocAPI.assets.cityRooms.QueryDataBySceneName(quitPoint.Item1, out var proto))
		{
			Debug.LogWarning("城镇交通线路中找不到虚空场景入口:" + quitPoint.Item1);
			return WrapTask(new NpcTaskFlash(string.Empty));
		}
		PathAction[] array = DolocAPI.assets.cityRooms.GeneratePathActionsEx(_npc.sceneName, proto.sceneInfo.name, _npc.positionWS, quitPoint.Item2);
		if (array == null)
		{
			Debug.LogWarning(_npc.NpcName + "找不到从" + _npc.sceneName + "前往虚空场景入口\"" + proto.name + "\"的路径");
			return WrapTask(new NpcTaskFlash(string.Empty));
		}
		LinearTask task = GetTaskFromPathActions(array).NpcDo(delegate(Npc npc)
		{
			npc.ManualSetPosition(string.Empty, Vector2.zero);
		});
		return WrapTask(task);
	}

	protected override LinearTask MakeDecision()
	{
		NpcScheduleWork work = _scheduleResult.work;
		if (work == null)
		{
			return DefaultTask;
		}
		LinearTask linearTask = work.CreateTask(_npc);
		if (linearTask == null)
		{
			return DefaultTask;
		}
		return WrapTask(linearTask);
	}
}
