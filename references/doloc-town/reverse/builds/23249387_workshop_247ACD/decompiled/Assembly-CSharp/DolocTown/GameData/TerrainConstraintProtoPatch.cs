using DolocTown.Config.Resource;
using UnityEngine;

namespace DolocTown.GameData;

public static class TerrainConstraintProtoPatch
{
	public static Vector2Int[] GetPositions(this TerrainConstraintProto proto, DungeonResourceType type)
	{
		switch (type)
		{
		case DungeonResourceType.TREE:
		case DungeonResourceType.TREE_TRUNK:
			return proto[DungeonResourceConstraintType.Tree];
		case DungeonResourceType.ORE:
			return proto[DungeonResourceConstraintType.Ore];
		case DungeonResourceType.SAND:
			return proto[DungeonResourceConstraintType.Sand];
		case DungeonResourceType.WEEDS:
		case DungeonResourceType.WEEDS_SMALL:
			return proto[DungeonResourceConstraintType.Weeds];
		case DungeonResourceType.PAPERBOX:
		case DungeonResourceType.MECHANICAL_REMAINS:
		case DungeonResourceType.BUILDING_REMAINS:
		case DungeonResourceType.HIVE:
			return proto[DungeonResourceConstraintType.Machine];
		default:
			return proto[4];
		}
	}

	public static Vector2Int[] GetPositions(this TerrainConstraintProto proto, VegetationType type)
	{
		return type switch
		{
			VegetationType.ENVIRONMENT => proto[VegetationConstraintType.Environment], 
			VegetationType.COLLECT => proto[VegetationConstraintType.Collect], 
			VegetationType.UNDER_WATER => proto[VegetationConstraintType.UnderWater], 
			VegetationType.LUMINOUS_PLANT => proto[VegetationConstraintType.LuminousPlant], 
			_ => proto[VegetationConstraintType.General], 
		};
	}

	public static Vector2Int[] GetPositions(this TerrainConstraintProto proto, EnvObjectType type)
	{
		if (type == EnvObjectType.BIRD)
		{
			return proto[EnvObjectType.BIRD];
		}
		return proto[0];
	}
}
