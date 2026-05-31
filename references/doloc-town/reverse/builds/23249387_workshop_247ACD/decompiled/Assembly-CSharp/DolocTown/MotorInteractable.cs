using DolocTown.Config;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class MotorInteractable : DolocObject, IInteractable, IAttackable
{
	private MotorController _motorController;

	private Shiner shiner;

	public bool OnlyTouch => false;

	public bool CanInteractContinues => false;

	AttackableType IAttackable.attackableType
	{
		get
		{
			if (!DolocAPI.IsAgentRiding)
			{
				return AttackableType.Unattackable;
			}
			return AttackableType.Player;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		shiner = new Shiner(GetComponentInParent<SpriteRenderer>(includeInactive: true));
		_motorController = GetComponentInParent<MotorController>(includeInactive: true);
	}

	public void OnTouch()
	{
		this.ShowSceneOperationTip(position2d + new Vector2(0f, 4.5f), DolocConfig.StaticTexts.UiOperationRide);
	}

	public void OnDisTouch()
	{
		this.HideSceneOperationTip();
	}

	public void OnInteract()
	{
		DolocAPI.gameStateManager.agentController.GetOnMotor();
	}

	bool IAttackable.OnAttacked(float attack, bool criticalRate, Vector2 position, out bool isDead)
	{
		isDead = false;
		if (!_motorController.IsRiding)
		{
			return false;
		}
		shiner.Raise(LocMaterials.GAME_MAT_HIT, 0.1f);
		DolocAPI.RaiseInstantAnimEffects(position, InstAnimEffectType.IMPACT_01);
		DolocAPI.gameStateManager.normalGameState.AgentController.GetOffMotor();
		DolocAPI.agent.OnAttacked(attack, criticalRate, position, out isDead);
		return true;
	}

	public bool OnSwordAttack(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		shiner.Raise(LocMaterials.GAME_MAT_HIT, 0.1f);
		DolocAPI.RaiseInstantAnimEffects(position, InstAnimEffectType.IMPACT_01);
		DolocAPI.gameStateManager.normalGameState.AgentController.GetOffMotor();
		DolocAPI.agent.OnSwordAttack(attack, criticalRate, pos, out isDead);
		return true;
	}
}
