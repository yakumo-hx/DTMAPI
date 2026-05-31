using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public interface IDecal : IHasIndex
{
	string Name { get; }

	Vector3 WorldPosition { get; }

	Room CurrentRoom { get; }

	bool Valid => !FitSlots.IsNullOrEmpty();

	DecalSlot[] FitSlots { get; }

	IDecalHost DecalHost { get; }

	int DecalSlotIndex => DecalInfo?.DecalSlotIndex ?? 0;

	DecalInfo DecalInfo { get; set; }

	void SetDecalHost(IDecalHost decalHost);

	bool HostFilter(IDecalHost host);

	void OnTakeOff();

	void RefreshPosition();
}
