using UnityEngine;

namespace DolocTown;

public class GameEffectsManager : GEMGameEffectsAttribute
{
	private readonly DolocGameAssets prefabId;

	public override string PrefabPath => prefabId.ToString();

	public override int Frequency { get; set; } = 30;


	public GameEffectsManager(string containerPath, DolocGameAssets prefabId)
		: base(containerPath)
	{
		this.prefabId = prefabId;
	}

	public override GameObject LoadPrefab()
	{
		return DolocAPI.GetAsset<GameObject>(prefabId);
	}
}
