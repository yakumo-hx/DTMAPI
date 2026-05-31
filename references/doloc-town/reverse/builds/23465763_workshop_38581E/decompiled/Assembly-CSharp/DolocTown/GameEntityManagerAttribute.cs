using UnityEngine;

namespace DolocTown;

public class GameEntityManagerAttribute : GEMGameObjectAttribute
{
	public readonly DolocGameAssets pfb;

	public override string PrefabPath => pfb.ToString();

	public GameEntityManagerAttribute(string containerPath, DolocGameAssets pfb)
		: base(containerPath)
	{
		this.pfb = pfb;
	}

	public override GameObject LoadPrefab()
	{
		return DolocAPI.GetAsset<GameObject>(pfb);
	}
}
