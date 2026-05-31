using System;
using UnityEngine;

namespace DolocTown;

public class AttackBehaviourRenderer : MonoBehaviour
{
	protected MonsterRenderer MonsterRenderer { get; private set; }

	public virtual void Init(MonsterRenderer renderer)
	{
		MonsterRenderer = renderer;
	}

	public virtual void OnAttackBegin(MonsterAttackId id, Type type, Transform target)
	{
	}

	public virtual void OnAttackEnd(MonsterAttackId id, Type type, Transform target)
	{
	}
}
