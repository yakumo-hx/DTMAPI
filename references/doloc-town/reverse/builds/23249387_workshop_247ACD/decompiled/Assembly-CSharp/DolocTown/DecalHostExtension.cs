using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public static class DecalHostExtension
{
	public static (int, Vector3)[] GetSlotWorldPos(this IDecalHost decalHost, DecalSlotType slotType)
	{
		List<(int, Vector3)> list = new List<(int, Vector3)>();
		DecalSlot[] containedSlots = decalHost.ContainedSlots;
		foreach (DecalSlot decalSlot in containedSlots)
		{
			if (decalSlot.SlotType == slotType)
			{
				Vector3 slotWorldPos = decalHost.GetSlotWorldPos(decalSlot);
				list.Add((decalSlot.Index, slotWorldPos));
			}
		}
		return list.ToArray();
	}

	public static bool TryGetSlotWorldPosByIndex(this IDecalHost decalHost, int index, out Vector3 worldPos)
	{
		worldPos = Vector3.zero;
		if (decalHost?.ContainedSlots == null || index < 0 || index >= decalHost.ContainedSlots.Length)
		{
			return false;
		}
		DecalSlot decalSlot = decalHost.ContainedSlots[index];
		worldPos = decalHost.GetSlotWorldPos(decalSlot);
		return true;
	}

	private static Vector3 GetSlotWorldPos(this IDecalHost decalHost, DecalSlot decalSlot)
	{
		if (decalHost.HostSprite == null)
		{
			return decalHost.WorldPosition;
		}
		float x = ((float)decalSlot.PixelPivot.x - decalHost.HostSprite.pivot.x) / decalHost.HostSprite.rect.size.x * decalHost.HostSprite.bounds.size.x;
		float y = ((float)decalSlot.PixelPivot.y - decalHost.HostSprite.pivot.y) / decalHost.HostSprite.rect.size.y * decalHost.HostSprite.bounds.size.y;
		Vector3 result = decalHost.WorldPosition + new Vector3(x, y, 0f);
		result.z = 0f - Mathf.Max(result.y - decalHost.WorldPosition.y, 0f) - 0.001f;
		return result;
	}

	public static int GetNearestSlotWorldPos(this IDecalHost decalHost, DecalSlotType slotType, Vector3 targetWorldPos, out Vector3 nearestWorldPos, out float distance)
	{
		distance = float.MaxValue;
		nearestWorldPos = Vector3.zero;
		int result = -1;
		DecalSlot[] containedSlots = decalHost.ContainedSlots;
		foreach (DecalSlot decalSlot in containedSlots)
		{
			if (decalSlot.SlotType == slotType)
			{
				float x = ((float)decalSlot.PixelPivot.x - decalHost.HostSprite.pivot.x) / decalHost.HostSprite.rect.size.x * decalHost.HostSprite.bounds.size.x;
				float y = ((float)decalSlot.PixelPivot.y - decalHost.HostSprite.pivot.y) / decalHost.HostSprite.rect.size.y * decalHost.HostSprite.bounds.size.y;
				Vector3 vector = decalHost.WorldPosition + new Vector3(x, y, 0f);
				float magnitude = (vector - targetWorldPos).magnitude;
				if (magnitude < distance)
				{
					result = decalSlot.Index;
					distance = magnitude;
					nearestWorldPos = vector;
				}
			}
		}
		return result;
	}

	public static bool _RemoveDecal(this IDecalHost decalHost, IDecal decal)
	{
		return decalHost.AttachedDecals.Remove(decal.DecalSlotIndex);
	}

	public static void _AttachDecal(this IDecalHost decalHost, IDecal decal)
	{
		if (decalHost.AttachedDecals.ContainsKey(decal.DecalSlotIndex))
		{
			Debug.LogWarning($"宿主{decal.Name}的贴纸槽{decal.DecalSlotIndex}已被占用，此次附加贴纸{decal.Name}会覆盖已有内容{decalHost.AttachedDecals[decal.DecalSlotIndex].Name}");
		}
		decalHost.AttachedDecals[decal.DecalSlotIndex] = decal;
	}

	public static bool TakeOffAllDecals(this IDecalHost decalHost, bool putInBackpack = false)
	{
		IDecal[] array = decalHost.AttachedDecals.Values.ToArray();
		for (int num = array.Length - 1; num >= 0; num--)
		{
			array[num].TakeOffDecal(putInBackpack);
		}
		decalHost.AttachedDecals.Clear();
		return array.Length != 0;
	}

	public static bool TakeOffDecalsOfType<T>(this IDecalHost decalHost, bool putInBackpack = false) where T : IDecal
	{
		bool result = false;
		for (int num = decalHost.AttachedDecals.Count - 1; num >= 0; num--)
		{
			if (decalHost.AttachedDecals[num] is T val)
			{
				val.TakeOffDecal(putInBackpack);
				decalHost.AttachedDecals.Remove(val.DecalSlotIndex);
				result = true;
			}
		}
		return result;
	}

	public static T[] GetDecalsOfType<T>(this IDecalHost decalHost) where T : IDecal
	{
		List<T> list = new List<T>();
		foreach (IDecal value in decalHost.AttachedDecals.Values)
		{
			if (value is T item)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}
}
