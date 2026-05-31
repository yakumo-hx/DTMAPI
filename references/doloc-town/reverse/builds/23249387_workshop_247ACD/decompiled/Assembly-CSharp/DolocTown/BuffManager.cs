using System.Collections.Generic;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class BuffManager
{
	private readonly struct BuffRequest
	{
		public readonly Buff buff;

		public readonly bool isAdd;

		public BuffRequest(Buff buff, bool isAdd)
		{
			this.buff = buff;
			this.isAdd = isAdd;
		}
	}

	[JsonProperty]
	private readonly Dictionary<string, Buff> loadedBuffs = new Dictionary<string, Buff>();

	private readonly Queue<BuffRequest> requestList = new Queue<BuffRequest>();

	private bool ProtectedMode;

	private AbilitySystem AbilitySystem => DolocAPI.AbilitySystem;

	public BuffManager()
	{
	}

	[JsonConstructor]
	private BuffManager(Dictionary<string, Buff> loadedBuffs)
	{
		this.loadedBuffs = loadedBuffs ?? new Dictionary<string, Buff>();
		foreach (Buff value in this.loadedBuffs.Values)
		{
			DolocAPI.AddBuffIcon(value);
			value.Apply();
		}
	}

	public bool Add(string id, float scale = 1f)
	{
		if (id.IsNullOrEmpty())
		{
			return false;
		}
		Buff buff = new Buff(id, scale);
		if (!buff.Valid)
		{
			return false;
		}
		Add(buff);
		return true;
	}

	public bool Remove(string id)
	{
		if (id == null || !loadedBuffs.TryGetValue(id, out var value))
		{
			return false;
		}
		_Remove(value);
		return true;
	}

	private void Add(Buff buff)
	{
		if (buff.Duration == 0)
		{
			buff.Apply();
			return;
		}
		if (ProtectedMode)
		{
			requestList.Enqueue(new BuffRequest(buff, isAdd: true));
			return;
		}
		DolocAPI.AddBuffIcon(buff);
		if (loadedBuffs.TryGetValue(buff.Id, out var value))
		{
			value.Timer.Refresh();
			return;
		}
		loadedBuffs.Add(buff.Id, buff);
		buff.Apply();
	}

	public bool HasBuff(string id)
	{
		return loadedBuffs.ContainsKey(id);
	}

	public void UpdatePerTU()
	{
		UpdatePerTUNoRender();
		_UpdateProgress();
	}

	public void UpdatePerTUNoRender()
	{
		_UpdatePerTU();
	}

	public void RefreshUI()
	{
		_UpdateProgress();
	}

	private void _UpdatePerTU()
	{
		if (loadedBuffs.Count == 0)
		{
			return;
		}
		ProtectedMode = true;
		foreach (Buff value in loadedBuffs.Values)
		{
			if (value.Timer.Update())
			{
				_Remove(value);
			}
		}
		ProtectedMode = false;
		while (requestList.Count > 0)
		{
			BuffRequest buffRequest = requestList.Dequeue();
			if (buffRequest.isAdd)
			{
				Add(buffRequest.buff);
			}
			else
			{
				_Remove(buffRequest.buff);
			}
		}
	}

	private void _UpdateProgress()
	{
		if (loadedBuffs.Count == 0)
		{
			return;
		}
		foreach (Buff value in loadedBuffs.Values)
		{
			DolocAPI.UpdateBuffProgress(value.Id, value.Timer.currentTick);
		}
	}

	private void _Remove(Buff buff)
	{
		if (ProtectedMode)
		{
			requestList.Enqueue(new BuffRequest(buff, isAdd: false));
			return;
		}
		loadedBuffs.Remove(buff.Id);
		DolocAPI.RemoveBuffIcon(buff.Id);
		buff.Remove();
		if (loadedBuffs.Count == 0)
		{
			AbilitySystem.Recalculate();
		}
	}

	public void ComposeHealthAdder(int value)
	{
		DolocAPI.AddHealth(value);
	}

	public void ComposeEnergyAdder(int value)
	{
		DolocAPI.AddEnergy(value);
	}

	public void ComposeSpiritAdder(int value)
	{
		DolocAPI.AddSpirit(value);
	}

	public void ComposeJumpAdder(int value)
	{
		float jumpAdder = AbilitySystem.motionAbility.JumpAdder + (float)value;
		AbilitySystem.motionAbility.SetJumpAdder(jumpAdder);
	}

	public void ComposeJumpScaler(float value)
	{
		float jumpScaler = AbilitySystem.motionAbility.JumpScaler + value;
		AbilitySystem.motionAbility.SetJumpScaler(jumpScaler);
	}

	public void ComposeMoveAdder(int value)
	{
		float moveAdder = AbilitySystem.motionAbility.MoveAdder + (float)value;
		AbilitySystem.motionAbility.SetMoveAdder(moveAdder);
	}

	public void ComposeMoveScaler(float value)
	{
		float moveScaler = AbilitySystem.motionAbility.MoveScaler + value;
		AbilitySystem.motionAbility.SetMoveScaler(moveScaler);
	}

	public void ComposeCollectWeedsScaler(float value)
	{
		float weedsScaler = AbilitySystem.collectionAbility.WeedsScaler;
		AbilitySystem.collectionAbility.SetCollectionWeedsScaler(weedsScaler + value);
	}

	public void ComposeCollectWeedsAdder(int value)
	{
		float weedsAdder = AbilitySystem.collectionAbility.WeedsAdder;
		AbilitySystem.collectionAbility.SetCollectionWeedsAdder(weedsAdder + (float)value);
	}

	public void ComposeCollectWoodsScaler(float value)
	{
		float woodsScaler = AbilitySystem.collectionAbility.WoodsScaler;
		AbilitySystem.collectionAbility.SetCollectionWoodsScaler(woodsScaler + value);
	}

	public void ComposeCollectWoodsAdder(int value)
	{
		float woodsAdder = AbilitySystem.collectionAbility.WoodsAdder;
		AbilitySystem.collectionAbility.SetCollectionWoodsAdder(woodsAdder + (float)value);
	}

	public void ComposeCollectStoneScaler(float value)
	{
		float stoneScaler = AbilitySystem.collectionAbility.StoneScaler;
		AbilitySystem.collectionAbility.SetCollectionStoneScaler(stoneScaler + value);
	}

	public void ComposeCollectStoneAdder(int value)
	{
		float stoneAdder = AbilitySystem.collectionAbility.StoneAdder;
		AbilitySystem.collectionAbility.SetCollectionStoneAdder(stoneAdder + (float)value);
	}

	public void ComposeCollectGarbageScaler(float value)
	{
		float garbageScaler = AbilitySystem.collectionAbility.GarbageScaler;
		AbilitySystem.collectionAbility.SetCollectionGarbageScaler(garbageScaler + value);
	}

	public void ComposeCollectGarbageAdder(int value)
	{
		float garbageAdder = AbilitySystem.collectionAbility.GarbageAdder;
		AbilitySystem.collectionAbility.SetCollectionGarbageAdder(garbageAdder + (float)value);
	}

	public void ComposeHealthRecoveryScaler(float value)
	{
		float healthScaler = AbilitySystem.recorveryAbility.HealthScaler;
		AbilitySystem.recorveryAbility.SetHealthScaler(healthScaler + value);
	}

	public void ComposeHealthRecoveryAdder(int value)
	{
		float healthAdder = AbilitySystem.recorveryAbility.HealthAdder;
		AbilitySystem.recorveryAbility.SetHealthAdder(healthAdder + (float)value);
	}

	public void ComposeEnergyRecoveryScaler(float value)
	{
		float energyScaler = AbilitySystem.recorveryAbility.EnergyScaler;
		AbilitySystem.recorveryAbility.SetEnergyScaler(energyScaler + value);
	}

	public void ComposeEnergyRecoveryAdder(int value)
	{
		float energyAdder = AbilitySystem.recorveryAbility.EnergyAdder;
		AbilitySystem.recorveryAbility.SetEnergyAdder(energyAdder + (float)value);
	}

	public void ComposeEnergySpiritScaler(float value)
	{
		float energyScaler = AbilitySystem.recorveryAbility.EnergyScaler;
		AbilitySystem.recorveryAbility.SetEnergyScaler(energyScaler + value);
	}

	public void ComposeEnergySpiritAdder(int value)
	{
		float energyAdder = AbilitySystem.recorveryAbility.EnergyAdder;
		AbilitySystem.recorveryAbility.SetEnergyAdder(energyAdder + (float)value);
	}

	public void ComposeDefendAdder(int value)
	{
		float defendAdder = AbilitySystem.battleAbility.DefendAdder;
		AbilitySystem.battleAbility.SetDefendAdder(defendAdder + (float)value);
	}

	public void ComposeDefendScaler(float value)
	{
		float defendScaler = AbilitySystem.battleAbility.DefendScaler;
		AbilitySystem.battleAbility.SetDefendScaler(defendScaler + value);
	}
}
