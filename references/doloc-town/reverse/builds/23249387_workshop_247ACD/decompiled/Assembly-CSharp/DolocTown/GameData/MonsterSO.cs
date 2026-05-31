using System;
using System.Collections.Generic;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "Monster", menuName = "多洛可小镇[地牢]/战斗系统/怪物")]
public class MonsterSO : SerializedScriptableObject
{
	[SerializeField]
	private MonsterMoverSO moverSO;

	[SerializeField]
	private MonsterAttackBehaviourSO[] attackBehaviours = Array.Empty<MonsterAttackBehaviourSO>();

	private IEnumerable<Type> AvailableAttackBehaviourTypes => typeof(MonsterAttackBehaviourSO).GetSubTypes();

	public void OnValidate()
	{
		if (!attackBehaviours.IsNullOrEmpty())
		{
			MonsterAttackBehaviourSO[] array = attackBehaviours;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnValidate();
			}
		}
	}

	public bool CreateProto(out MonsterProto proto)
	{
		proto = default(MonsterProto);
		if (moverSO == null || !moverSO.CreateProto(out var proto2))
		{
			return false;
		}
		string text = base.name;
		MonsterMoverProto moverProto = proto2;
		IMonsterAttackBehaviour[] attackBehaviourProtos = attackBehaviours;
		proto = new MonsterProto(text, moverProto, attackBehaviourProtos);
		return true;
	}
}
