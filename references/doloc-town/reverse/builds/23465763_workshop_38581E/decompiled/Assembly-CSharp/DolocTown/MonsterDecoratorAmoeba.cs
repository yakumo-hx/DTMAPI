using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorAmoeba : MonsterDecorator
{
	[SerializeField]
	[Range(0.1f, 1f)]
	private float hurtDuration = 0.3f;

	[SerializeField]
	[Range(1f, 30f)]
	private float backForce = 5f;

	protected override void OnInitDecorator()
	{
		base.Controller.OnHurtResumed += delegate
		{
			GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			base.Controller.Renderer.PlayAnimation("idle");
		};
	}

	public override void OnRecycle()
	{
		PhysicalDamageBox component = GetComponent<PhysicalDamageBox>();
		if (!(component == null))
		{
			component.Enabled = false;
		}
	}

	public override bool OnFell(ItemTool tool, Vector2 pos)
	{
		if (base.Monster == null)
		{
			return false;
		}
		if (!base.Monster.Damage(tool.Attack, critical: false, out var value))
		{
			base.Controller.ReduceCD(MonsterAttackId.AtkNormal, 1f);
			base.Controller.Hurt(DolocAPI.AgentTransform, hurtDuration);
			if (!base.Controller.attackBehaviourManager.IsAttacking)
			{
				base.Controller.Renderer.PlayAnimation("hurt");
			}
			Rigidbody2D component = GetComponent<Rigidbody2D>();
			Vector2 force = new Vector2(Mathf.Sign(base.transform.position.x - pos.x) * backForce, 0f);
			component.AddForce(force, ForceMode2D.Impulse);
		}
		DolocAPI.RaiseDamageTip(value, pos);
		return true;
	}
}
