using System.Collections.Generic;
using DolocTown.Config.Fishing;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[NpcTaskRenderController("lightman")]
public class NpcTaskRenderController_Lightman : NpcTaskRenderController
{
	private readonly Counter _iconCounter = new Counter();

	private LightManFishingRope _fishingRope;

	private string currentFishingPoolName;

	private Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();

	private void ResetCounter()
	{
		int num = DolocAPI.GlobalParameter.NpcRelaxLightmanFishingInterval.Random();
		_iconCounter.SetInterval(num * DolocAPI.GlobalParameter.TULength);
	}

	public override void OnRenderNpc()
	{
		base.OnRenderNpc();
		_fishingRope = DolocAPI.EntitySystem.Next<LightManFishingRope>();
		_fishingRope.transform.SetParent(base.npc.Renderer.transform);
		_fishingRope.transform.localScale = Vector3.one;
		_fishingRope.SetVisible(value: false);
		if (base.npc.controller.CurrentTask is NpcWorkTask { WorkType: NpcScheduleWorkType.Relax })
		{
			base.npc.Renderer.PlayAnimation("relax", force: true);
			OnUseFishingRope();
		}
	}

	public override void OnUnRenderNpc()
	{
		base.OnUnRenderNpc();
		DolocAPI.EntitySystem.Recycle(_fishingRope);
		GameObject container = DolocAPI.EntitySystem.GetContainer<LightManFishingRope>();
		if (container != null && _fishingRope != null)
		{
			_fishingRope.transform.SetParent(container.transform);
			_fishingRope.transform.localScale = Vector3.one;
		}
	}

	private WaterController FindWaterController()
	{
		GameEntityManager<WaterRenderer> gameEntityManager = DolocAPI.EntitySystem.GetGameEntityManager<WaterRenderer>();
		if (gameEntityManager == null)
		{
			Debug.LogError("<color=red>没有找到水体渲染器，无法使用钓鱼绳!</color>");
			return null;
		}
		if (gameEntityManager.ActivedGameEntityCount == 0)
		{
			return null;
		}
		if (gameEntityManager.ActivedGameEntityCount == 1)
		{
			return gameEntityManager.Pool.Instances[0].WaterController;
		}
		float num = float.MaxValue;
		WaterController result = null;
		foreach (WaterRenderer instance in gameEntityManager.Pool.Instances)
		{
			float num2 = Vector3.Distance(base.npc.Renderer.position2d, instance.transform.position);
			if (num2 < num)
			{
				num = num2;
				result = instance.WaterController;
			}
		}
		return result;
	}

	private string FindFishingPool()
	{
		if (!base.npc.TryGetCurrentRoom(out var room) || DolocAPI.CurrentRoom != room)
		{
			return null;
		}
		ISceneHandle sceneHandle = room.SceneHandle;
		if (sceneHandle == null)
		{
			return null;
		}
		FishingPool[] componentsInScene = sceneHandle.GetComponentsInScene<FishingPool>();
		if (componentsInScene.Length == 0)
		{
			return null;
		}
		float num = float.MaxValue;
		FishingPool fishingPool = null;
		FishingPool[] array = componentsInScene;
		foreach (FishingPool fishingPool2 in array)
		{
			float num2 = Vector3.Distance(base.npc.Renderer.position2d, fishingPool2.transform.position);
			if (num2 < num)
			{
				num = num2;
				fishingPool = fishingPool2;
			}
		}
		if (!(fishingPool != null))
		{
			return null;
		}
		return fishingPool.PoolName;
	}

	private void OnUseFishingRope()
	{
		if (base.npc.IsRenderNow)
		{
			currentFishingPoolName = FindFishingPool();
			FloatingObjectBase componentInChildren = _fishingRope.GetComponentInChildren<FloatingObjectBase>();
			componentInChildren._controller = FindWaterController();
			componentInChildren.positionLocal = new Vector3(5f, -3f, 0f);
			if (!_fishingRope.isVisible)
			{
				_fishingRope.transform.localPosition = Vector3.zero;
				_fishingRope._ropeRenderer.ResetRope();
				_fishingRope.GetComponent<Animator>().Play("relax", 0);
				_fishingRope.SetVisible(value: true);
			}
			ResetCounter();
		}
	}

	private bool TryGetFishIcon(FishInfo proto, out Sprite icon)
	{
		icon = null;
		if (proto == null)
		{
			return false;
		}
		if (_iconCache.TryGetValue(proto.Id, out icon))
		{
			return icon != null;
		}
		if (!DolocAPI.QueryItemProto(proto.Id, out var proto2))
		{
			_iconCache[proto.Id] = null;
			return false;
		}
		icon = proto2.UiSpriteAsset.Asset;
		_iconCache[proto.Id] = icon;
		return icon != null;
	}

	private void CatchFish()
	{
		if (!base.npc.IsRenderNow)
		{
			return;
		}
		if (currentFishingPoolName == null)
		{
			currentFishingPoolName = FindFishingPool();
			if (currentFishingPoolName == null)
			{
				return;
			}
		}
		FishInfo fishInfo = DolocAPI.RollFish(currentFishingPoolName, 4);
		if (fishInfo == null || !TryGetFishIcon(fishInfo, out var icon))
		{
			return;
		}
		DolocAPI.RaiseSpriteFadeUp(base.npc.Renderer.UiPopPositionWS, icon, 1.25f);
		if (fishInfo.IsGarbage)
		{
			if (RandomUtils.Dice(0.7f))
			{
				EmotionName name = (RandomUtils.Dice(0.5f) ? EmotionName.NOCOMMENT : EmotionName.ANGRY);
				DolocAPI.RaiseEmotion(base.npc.Renderer.transform, name);
			}
		}
		else if (RandomUtils.Dice(0.7f))
		{
			EmotionName name2 = (RandomUtils.Dice(0.5f) ? EmotionName.LOVE : EmotionName.LAUGH);
			DolocAPI.RaiseEmotion(base.npc.Renderer.transform, name2);
		}
	}

	private void OnStopUseFishingRope()
	{
		if (base.npc.IsRenderNow)
		{
			_fishingRope.SetVisible(value: false);
			base.npc.Renderer.PlayAnimation("idle", force: true);
		}
	}

	public override void OnTaskBegin(NpcScheduleWorkType workType)
	{
		base.OnTaskBegin(workType);
		if (workType == NpcScheduleWorkType.Relax)
		{
			OnUseFishingRope();
		}
	}

	public override void OnTaskBreak(NpcScheduleWorkType workType)
	{
		base.OnTaskBreak(workType);
		if (workType == NpcScheduleWorkType.Relax)
		{
			OnStopUseFishingRope();
		}
	}

	public override void OnTaskExecute(NpcScheduleWorkType workType)
	{
		if (!(base.npc.Renderer == null))
		{
			base.OnTaskExecute(workType);
			if (_iconCounter.Tick())
			{
				ResetCounter();
				CatchFish();
				base.npc.Renderer.PostSoundEventByEnum(SoundEvents.PLAY_ITEM_PICK_UP);
			}
		}
	}
}
