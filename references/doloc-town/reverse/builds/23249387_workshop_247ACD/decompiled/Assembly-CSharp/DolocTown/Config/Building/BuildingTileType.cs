using System;

namespace DolocTown.Config.Building;

[Flags]
public enum BuildingTileType
{
	None = 0,
	NoOverlay = 1,
	Front = 2,
	Side = 4,
	Ceiling = 8,
	Floor = 0x10,
	Fulcrum = 0x20,
	Door = 0x40
}
