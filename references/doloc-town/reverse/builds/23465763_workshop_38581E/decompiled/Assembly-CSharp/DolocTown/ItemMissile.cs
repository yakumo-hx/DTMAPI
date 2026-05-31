using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemMissile : Item
{
	public override bool availableIfRiding => true;

	private bool ShouldNotThrow => DolocAPI.IsAgentRiding;

	public ItemMissile(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemMissile(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		if (!DolocAPI.userInput.CheckState<NormalGameState>() || (!DolocAPI.IsAgentRiding && !DolocAPI.agent.IsCurrentStateSupportUseItem))
		{
			return;
		}
		CostSelf(showFadeUpIcon: false);
		if (!(base.proto.Function is ItemFunctionMissile itemFunctionMissile))
		{
			Debug.LogWarning("ItemFunctionMissile is null");
			return;
		}
		Skill skill = DolocAPI.gameStateManager.agentController.skillManager.MakeSkillObject(itemFunctionMissile.Name);
		if (skill == null)
		{
			return;
		}
		if (!(skill is TimeBomb timeBomb))
		{
			if (!(skill is Bomb bomb))
			{
				if (skill is Firecracker firecracker)
				{
					HandleFireCracker(itemFunctionMissile, firecracker);
				}
			}
			else
			{
				HandleBomb(itemFunctionMissile, bomb);
			}
		}
		else
		{
			HandleTimeBomb(itemFunctionMissile, timeBomb);
		}
	}

	private void HandleTimeBomb(ItemFunctionMissile itemFunction, TimeBomb timeBomb)
	{
		timeBomb.Damage = itemFunction.Damage;
		timeBomb.CriticalRate = itemFunction.CriticalRate;
		timeBomb.ShouldWorkOnMonster = true;
		timeBomb.IsItemBomb = true;
		timeBomb.position2d = DolocAPI.agent.PositionCenter;
	}

	private void HandleBomb(ItemFunctionMissile itemFunction, Bomb bomb)
	{
		bomb.SetVisible(value: false);
		bomb.position2d = DolocAPI.agent.PositionThrow;
		bomb.ShouldWorkOnMonster = true;
		bomb.Damage = itemFunction.Damage;
		bomb.CriticalRate = itemFunction.CriticalRate;
		if (ShouldNotThrow)
		{
			bomb.SetVisible(value: true);
			bomb.position2d = DolocAPI.agent.PositionThrow;
			bomb.AddForce(new Vector2(DolocAPI.Motor.IsFaceRight ? 1 : (-1), 2f) * 16f);
			return;
		}
		DolocAPI.agent._Throw(delegate(bool isFaceRight)
		{
			bomb.SetVisible(value: true);
			bomb.position2d = DolocAPI.agent.PositionThrow;
			bomb.AddForce(new Vector2(isFaceRight ? 1 : (-1), 2f) * 16f);
		});
	}

	private void HandleFireCracker(ItemFunctionMissile itemFunction, Firecracker firecracker)
	{
		firecracker.SetVisible(value: false);
		firecracker.position2d = DolocAPI.agent.PositionThrow;
		firecracker.Damage = itemFunction.Damage;
		firecracker.CriticalRate = itemFunction.CriticalRate;
		if (ShouldNotThrow)
		{
			firecracker.SetVisible(value: true);
			firecracker.position2d = DolocAPI.agent.PositionThrow;
			firecracker.AddForce(new Vector2(DolocAPI.Motor.IsFaceRight ? 1 : (-1), 1.5f) * 20f);
			return;
		}
		DolocAPI.agent._Throw(delegate(bool isFaceRight)
		{
			firecracker.SetVisible(value: true);
			firecracker.position2d = DolocAPI.agent.PositionThrow;
			firecracker.AddForce(new Vector2(isFaceRight ? 1 : (-1), 1.5f) * 20f);
		});
	}
}
