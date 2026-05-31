using UnityEngine;

namespace DolocTown;

public class MonsterDecorator : MonoBehaviour
{
	protected MonsterController Controller { get; private set; }

	protected MonsterRenderer Renderer { get; private set; }

	protected Monster Monster => Controller.Monster;

	public virtual bool ShieldBullet => false;

	public virtual Vector2 FirePosition => base.transform.position;

	public virtual bool ShouldGenDropItems => true;

	public virtual bool SendDeadEvent => true;

	public virtual AttackableType attackableType => AttackableType.Enemy;

	public void Init(MonsterController host)
	{
		Controller = host;
		Renderer = host.Renderer;
		OnInitDecorator();
	}

	protected virtual void OnInitDecorator()
	{
	}

	public virtual void OnMonsterLoaded(Monster monster)
	{
	}

	public virtual void OnUpdate(float dt)
	{
	}

	public virtual void OnFixedUpdate(float dt)
	{
	}

	public virtual void OnEnvironmentChanged(MonsterEnv env)
	{
	}

	public virtual void OnRecycle()
	{
	}

	public virtual void OnReuse()
	{
	}

	public virtual bool OnFell(ItemTool tool, Vector2 pos)
	{
		if (!Controller.Monster.Damage(tool.Attack, critical: false, out var value))
		{
			Controller.Hurt(DolocAPI.AgentTransform);
		}
		DolocAPI.RaiseDamageTip(value, pos);
		return true;
	}

	public virtual void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		OnAttacked(damage, criticalRate, pos, out var _);
		Controller.StopAttack();
	}

	public virtual bool OnAttacked(float attack, bool isCritical, Vector2 pos, out bool isDead)
	{
		isDead = false;
		if (Monster == null)
		{
			return false;
		}
		if (Monster.Damage(attack, isCritical, out var value))
		{
			isDead = true;
		}
		else
		{
			Controller.Hurt(DolocAPI.AgentTransform);
		}
		DolocAPI.RaiseDamageTip(value, pos, isCritical);
		return true;
	}

	public virtual bool OnSwordAttack(float atk, bool isCritical, Vector2 pos, out bool isDead)
	{
		return OnAttacked(atk, isCritical, pos, out isDead);
	}

	public virtual void OnDead()
	{
	}
}
