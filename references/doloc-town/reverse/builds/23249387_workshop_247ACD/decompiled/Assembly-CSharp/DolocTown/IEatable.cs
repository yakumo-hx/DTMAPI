using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Buff;
using DolocTown.Config.Item;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public interface IEatable
{
	bool isValid
	{
		get
		{
			if (effectProto != null)
			{
				return eatableItem != null;
			}
			return false;
		}
	}

	Item eatableItem { get; }

	EatingEffectInfo effectProto { get; }

	void Eat()
	{
		if (!DolocAPI.IsCurrentStateSupportInteract || effectProto == null)
		{
			return;
		}
		DolocAPI.gameStateManager.agentController.EnterState<AgentStateEat>();
		eatableItem.CostSelf();
		DolocAPI.Sound.PostSoundEvent(effectProto.SoundEvent);
		DolocAPI.Broadcast(OperationEventType.USE_ITEM);
		DolocAPI.BroadcastString(GameEventType.USE_ITEM, eatableItem.name);
		DolocAPI.effectProvider.RaiseInstPS(DolocAPI.agent.PositionCenter, InstantParticleEffectsType.SPARKS);
		DoEffects();
		if (effectProto.OutputItems.IsNullOrEmpty())
		{
			return;
		}
		CountItem[] outputItems = effectProto.OutputItems;
		for (int i = 0; i < outputItems.Length; i++)
		{
			CountItem countItem = outputItems[i];
			if (countItem.isValid)
			{
				DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, countItem, DolocAPI.AgentPosition);
			}
		}
	}

	void DoEffects(float scale = 1f)
	{
		FoodEffect[] allEffects = GetAllEffects();
		foreach (FoodEffect obj in allEffects)
		{
			int num = Mathf.RoundToInt(obj.Scale * scale);
			DolocAPI.AddBuff(obj.Buff, num);
			int valueInGame = obj.Buff_Ref.GetValueInGame(num);
			DolocAPI.RaiseUiEffects(valueInGame, obj.Buff_Ref.EffectType switch
			{
				BuffEffectType.Energy => DolocUiColor.AGENT_ENERGY, 
				BuffEffectType.Spirit => DolocUiColor.AGENT_SPIRIT, 
				BuffEffectType.Health => DolocUiColor.AGENT_HEALTH, 
				_ => DolocUiColor.TEXTCOLOR_STD, 
			});
		}
	}

	FoodEffect[] GetAllEffects()
	{
		if (effectProto == null || effectProto.Effects.IsNullOrEmpty())
		{
			return Array.Empty<FoodEffect>();
		}
		return effectProto.Effects.Concat(FetchExtraEffects()).ToArray();
	}

	FoodEffect[] FetchExtraEffects()
	{
		List<FoodEffect> list = new List<FoodEffect>();
		FoodEffect extraEffects_BetterWater = GetExtraEffects_BetterWater();
		if (extraEffects_BetterWater != null)
		{
			list.Add(extraEffects_BetterWater);
		}
		return list.ToArray();
	}

	FoodEffect GetExtraEffects_BetterWater()
	{
		if (!DolocAPI.archiveHandle.IsBetterWaterUnlocked())
		{
			return null;
		}
		if (!DolocAPI.GlobalParameter.WaterItems.Contains(eatableItem.name.ToLower()))
		{
			return null;
		}
		return DolocAPI.GlobalParameter.BetterWaterExtraBuff;
	}
}
