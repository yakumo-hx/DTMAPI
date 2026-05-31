using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public interface IDecalHost : IHasIndex
{
	bool Valid => !ContainedSlots.IsNullOrEmpty();

	string Name { get; }

	DecalSlot[] ContainedSlots { get; }

	Dictionary<int, IDecal> AttachedDecals { get; set; }

	Sprite HostSprite { get; }

	Vector3 WorldPosition { get; }
}
