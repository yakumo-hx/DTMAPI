using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class Monster : IBulletTrackingObject
{
	public readonly MonsterProto proto;

	private int currentHealth;

	private bool isDead;

	private int deadCount;

	public string Name => proto.Name;

	public IMonsterHost Host { get; set; }

	public MonsterController Controller { get; set; }

	public Vector2 position { get; set; }

	public bool HasBeenAttacked { get; private set; }

	public float HealthPercent => (float)currentHealth / (float)proto.Health;

	bool IBulletTrackingObject.isValid => Controller != null;

	Transform IBulletTrackingObject.transform => Controller.transform;

	public Monster(MonsterProto proto)
	{
		this.proto = proto;
		currentHealth = proto.Health;
		isDead = false;
		HasBeenAttacked = false;
	}

	private void HandleBeenAttacked()
	{
		if (!HasBeenAttacked)
		{
			HasBeenAttacked = true;
			if (DolocAPI.AgentEquipmentParams.ShieldSightOfMonsterNames.Contains(proto.Name))
			{
				DolocAPI.RaiseEmotionLimited(Controller.transform, EmotionName.CONFUSE);
			}
		}
	}

	public bool ManualDamage(int dmg)
	{
		currentHealth -= dmg;
		if (currentHealth > 0)
		{
			return false;
		}
		Kill();
		return true;
	}

	public bool Damage(float attack, bool critical, out int value)
	{
		HandleBeenAttacked();
		value = BattleUtils.CalcDamage(attack, proto.Defense, critical);
		currentHealth -= value;
		if (currentHealth > 0)
		{
			return false;
		}
		Kill();
		return true;
	}

	public bool Damage(float attack, bool critical, float defend, out int value)
	{
		HandleBeenAttacked();
		value = BattleUtils.CalcDamage(attack, defend, critical);
		currentHealth -= value;
		if (currentHealth > 0)
		{
			return false;
		}
		Kill();
		return true;
	}

	public void InvokeDeadEvents()
	{
		if (Controller.Decorator.SendDeadEvent)
		{
			DolocAPI.BroadcastString(GameEventType.SLAIN_MONSTER, proto.Name);
			DolocAPI.AddBattleExp(proto.ExpValue);
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Monster, proto.Name);
		}
	}

	public void InvokeDropLib()
	{
		if (Controller.Decorator.ShouldGenDropItems)
		{
			DolocAPI.SpawnMonsterDropItems(this);
		}
	}

	public void InvokeDeadEffects(Vector2 currentPosition)
	{
		Controller.PostSoundEvent(proto.DeadSound);
		proto.DeadEffects.Raise(currentPosition);
	}

	public bool Remove()
	{
		if (Controller == null || isDead)
		{
			return false;
		}
		isDead = true;
		Controller.OnDead();
		UniTask.DelayFrame(1).ContinueWith(delegate
		{
			Host.RemoveMonster(this);
		}).Forget();
		return true;
	}

	public void Kill()
	{
		if (Remove())
		{
			InvokeDeadEffects(Controller.position);
			InvokeDeadEvents();
			InvokeDropLib();
		}
	}
}
