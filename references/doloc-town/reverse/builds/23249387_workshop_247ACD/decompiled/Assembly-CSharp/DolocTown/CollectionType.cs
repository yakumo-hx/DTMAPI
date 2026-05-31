using System;

namespace DolocTown;

[Flags]
public enum CollectionType
{
	None = 0,
	Item = 1,
	Animal = 2,
	Fish = 4,
	Monster = 8,
	Npc = 0x10,
	Product = 0x20,
	Resource = 0x40,
	All = 0x7F
}
