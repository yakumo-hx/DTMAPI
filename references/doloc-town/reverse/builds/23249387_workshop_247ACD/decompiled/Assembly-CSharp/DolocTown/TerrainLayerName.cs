using System;

namespace DolocTown;

[Flags]
public enum TerrainLayerName
{
	None = 0,
	Ground = 1,
	PlatformSurface = 2,
	PlatformColumn = 4,
	Equipment = 8,
	ResourceTree = 0x10,
	ResourceOther = 0x20,
	Vegetation = 0x40,
	Door = 0x80,
	Decal = 0x100,
	BuildingFront = 0x200,
	BuildingSide = 0x400,
	CeilingFront = 0x800,
	CeilingSide = 0x1000,
	BuildingNoOverlay = 0x2000,
	ExtraObstacles = 0x4000,
	Ceiling = 0x1800,
	Building = 0x600,
	Structure = 3,
	Resource = 0x30,
	Platform = 6
}
