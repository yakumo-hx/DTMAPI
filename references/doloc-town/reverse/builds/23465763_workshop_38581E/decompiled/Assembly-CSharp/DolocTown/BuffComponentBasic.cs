using System;
using DolocTown.Config.Buff;
using UnityEngine;

namespace DolocTown;

public class BuffComponentBasic : BuffComponent
{
	private BuffBasicType type;

	private float value;

	private float scale;

	private AbilitySystem abilitySystem => DolocAPI.AbilitySystem;

	public BuffComponentBasic(BuffBasicType type, float value, float scale)
	{
		this.type = type;
		this.value = value;
		this.scale = scale;
	}

	private void Apply(float sign)
	{
		float num = value * scale * sign;
		switch (type)
		{
		case BuffBasicType.HealthAdder:
			DolocAPI.ChangeHealth(Mathf.RoundToInt(num), HurtReason.BadFood);
			break;
		case BuffBasicType.EnergyAdder:
			DolocAPI.ChangeEnergy(Mathf.RoundToInt(num));
			break;
		case BuffBasicType.SpiritAdder:
			DolocAPI.ChangeSpirit(Mathf.RoundToInt(num));
			break;
		case BuffBasicType.MoveScaler:
		{
			float spiritAdder = abilitySystem.motionAbility.MoveScaler + num;
			abilitySystem.motionAbility.SetMoveScaler(spiritAdder);
			break;
		}
		case BuffBasicType.MoveAdder:
		{
			float spiritAdder = abilitySystem.motionAbility.MoveAdder + num;
			abilitySystem.motionAbility.SetMoveAdder(spiritAdder);
			break;
		}
		case BuffBasicType.JumpScaler:
		{
			float spiritAdder = abilitySystem.motionAbility.JumpScaler + num;
			abilitySystem.motionAbility.SetJumpScaler(spiritAdder);
			break;
		}
		case BuffBasicType.JumpAdder:
		{
			float spiritAdder = abilitySystem.motionAbility.JumpAdder + num;
			abilitySystem.motionAbility.SetJumpAdder(spiritAdder);
			break;
		}
		case BuffBasicType.WeedsCollectionScaler:
		{
			float spiritAdder = abilitySystem.collectionAbility.WeedsScaler;
			abilitySystem.collectionAbility.SetCollectionWeedsScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.WeedsCollectionAdder:
		{
			float spiritAdder = abilitySystem.collectionAbility.WeedsAdder;
			abilitySystem.collectionAbility.SetCollectionWeedsAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.WoodsCollectionScaler:
		{
			float spiritAdder = abilitySystem.collectionAbility.WoodsScaler;
			abilitySystem.collectionAbility.SetCollectionWoodsScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.WoodsCollectionAdder:
		{
			float spiritAdder = abilitySystem.collectionAbility.WoodsAdder;
			abilitySystem.collectionAbility.SetCollectionWoodsAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.StoneCollectionScaler:
		{
			float spiritAdder = abilitySystem.collectionAbility.StoneScaler;
			abilitySystem.collectionAbility.SetCollectionStoneScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.StoneCollectionAdder:
		{
			float spiritAdder = abilitySystem.collectionAbility.StoneAdder;
			abilitySystem.collectionAbility.SetCollectionStoneAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.GarbageCollectionScaler:
		{
			float spiritAdder = abilitySystem.collectionAbility.GarbageScaler;
			abilitySystem.collectionAbility.SetCollectionGarbageScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.GarbageCollectionAdder:
		{
			float spiritAdder = abilitySystem.collectionAbility.GarbageAdder;
			abilitySystem.collectionAbility.SetCollectionGarbageAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.RecoveryHealthScaler:
		{
			float spiritAdder = abilitySystem.recorveryAbility.HealthScaler;
			abilitySystem.recorveryAbility.SetHealthScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.RecoveryHealthAdder:
		{
			float spiritAdder = abilitySystem.recorveryAbility.HealthAdder;
			abilitySystem.recorveryAbility.SetHealthAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.RecoveryEnergyScaler:
		{
			float spiritAdder = abilitySystem.recorveryAbility.EnergyScaler;
			abilitySystem.recorveryAbility.SetEnergyScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.RecoveryEnergyAdder:
		{
			float spiritAdder = abilitySystem.recorveryAbility.EnergyAdder;
			abilitySystem.recorveryAbility.SetEnergyAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.DefendAdder:
		{
			float spiritAdder = abilitySystem.battleAbility.DefendAdder;
			abilitySystem.battleAbility.SetDefendAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.DefendScaler:
		{
			float spiritAdder = abilitySystem.battleAbility.DefendScaler;
			abilitySystem.battleAbility.SetDefendScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.RecoverySpiritScaler:
		{
			float spiritAdder = abilitySystem.recorveryAbility.SpiritScaler;
			abilitySystem.recorveryAbility.SetSpiritScaler(spiritAdder + num);
			break;
		}
		case BuffBasicType.RecoverySpiritAdder:
		{
			float spiritAdder = abilitySystem.recorveryAbility.SpiritAdder;
			abilitySystem.recorveryAbility.SetSpiritAdder(spiritAdder + num);
			break;
		}
		case BuffBasicType.ImmuneAcidRainAdder:
			abilitySystem.stateAbility.ComposeImmuneAcidRainCounter((int)sign);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public override void Apply()
	{
		Apply(1f);
	}

	public override void Remove()
	{
		Apply(-1f);
	}
}
