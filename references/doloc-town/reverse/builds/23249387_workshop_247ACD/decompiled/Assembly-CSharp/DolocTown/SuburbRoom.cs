using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class SuburbRoom : CityRoom
{
	public SuburbRoom(RoomProto proto)
		: base(proto)
	{
	}

	[JsonConstructor]
	public SuburbRoom(string ProtoName, DropItemManager DM_dropitem, MissionItemManager DM_missionItem, DungeonResourceManager DM_dungeonResource, VegetationManager DM_vegetation, int stopTime, bool blockCreate, PlatformManager DM_platform = null, BuildingManager DM_building = null, EquipmentManager DM_equipment = null, AnimalManager DM_animal = null)
		: base(ProtoName, stopTime, DM_missionItem, DM_dropitem, null, DM_dungeonResource, DM_vegetation, DM_platform, DM_building, DM_equipment, DM_animal, blockCreate)
	{
	}
}
