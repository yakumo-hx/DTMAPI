using UnityEngine;

namespace DolocTown.GameData;

public class SkillProto
{
	public readonly string id;

	public readonly GameObject prefab;

	public SkillProto(string id, GameObject prefab)
	{
		this.id = id;
		this.prefab = prefab;
	}
}
