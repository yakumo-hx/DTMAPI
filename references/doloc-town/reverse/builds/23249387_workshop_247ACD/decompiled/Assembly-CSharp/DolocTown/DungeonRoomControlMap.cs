using UnityEngine.Tilemaps;

namespace DolocTown;

public class DungeonRoomControlMap
{
	public readonly Tilemap baseMap;

	public readonly Tilemap resourceLayerTree;

	public readonly Tilemap resourceLayerOther;

	public readonly Tilemap vegetationLayer;

	public readonly Tilemap monsterLayer;

	public readonly Tilemap collisionLayer;

	public readonly Tilemap envObjectLayer;

	public DungeonRoomControlMap(Tilemap baseMap, Tilemap resourceLayerTree, Tilemap resourceLayerOther, Tilemap vegetationLayer, Tilemap monsterLayer, Tilemap collisionLayer, Tilemap envObjectLayer)
	{
		this.baseMap = baseMap;
		this.resourceLayerTree = resourceLayerTree;
		this.resourceLayerOther = resourceLayerOther;
		this.vegetationLayer = vegetationLayer;
		this.monsterLayer = monsterLayer;
		this.collisionLayer = collisionLayer;
		this.envObjectLayer = envObjectLayer;
	}
}
