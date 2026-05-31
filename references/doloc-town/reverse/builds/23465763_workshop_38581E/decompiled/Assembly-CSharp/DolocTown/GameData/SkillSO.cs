using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "Skill", menuName = "多洛可小镇[地牢]/战斗系统/技能")]
public class SkillSO : SerializedScriptableObject
{
	[SerializeField]
	private GameObject prefab;

	public string SkillId => base.name;

	private bool IsPrefabValid(GameObject prefab)
	{
		if (prefab != null)
		{
			return prefab.GetComponent<Skill>();
		}
		return false;
	}

	public bool CreateProto(out SkillProto proto)
	{
		if (prefab == null || !prefab.GetComponent<Skill>())
		{
			proto = null;
			return false;
		}
		proto = new SkillProto(SkillId, prefab);
		return true;
	}
}
