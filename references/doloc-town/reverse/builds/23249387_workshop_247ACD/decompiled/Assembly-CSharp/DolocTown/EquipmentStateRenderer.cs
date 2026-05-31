using UnityEngine;

namespace DolocTown;

[GameEntityManager("/sub_entity/equipment_state", DolocGameAssets.GAME_ENTITY_EQUIPMENT_RUNNING_TIP)]
[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class EquipmentStateRenderer : GameEntity
{
	[SerializeField]
	private Animator animator;

	public void Hide()
	{
		SetVisible(value: false);
	}

	public void SetAsRunning()
	{
		SetVisible(value: true);
		animator.Play("running");
	}

	public void SetAsIdle()
	{
		SetVisible(value: true);
		animator.Play("idle");
	}

	public void SetAsLowPower()
	{
		SetVisible(value: true);
		animator.Play("low_power");
	}

	public void SetState(bool value)
	{
		if (value)
		{
			SetAsRunning();
		}
		else
		{
			SetAsLowPower();
		}
	}

	public void SetAsCorroded()
	{
		SetVisible(value: true);
		animator.Play("corroded");
	}

	public void Normal(bool isCorroded, bool isWorking, bool isIdle)
	{
		if (isCorroded)
		{
			SetVisible(value: true);
			animator.Play("corroded");
		}
		else if (isWorking)
		{
			SetVisible(value: true);
			animator.Play(isIdle ? "idle" : "running");
		}
		else
		{
			SetVisible(value: false);
		}
	}
}
