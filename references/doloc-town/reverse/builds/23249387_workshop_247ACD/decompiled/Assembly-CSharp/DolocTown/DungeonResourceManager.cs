using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Resource;
using DolocTown.Config.Time;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DungeonResourceManager
{
	public static readonly Dictionary<DungeonResourceType, Type> DungeonResourceTypes = new Dictionary<DungeonResourceType, Type>();

	[JsonProperty]
	private readonly Counter counterGrow = new Counter();

	[JsonProperty]
	private readonly Counter counterGenerator = new Counter();

	[JsonProperty]
	private readonly IndexList<DungeonResource> entities = new IndexList<DungeonResource>();

	public int ResourceTotalCount => entities.Count;

	public Dictionary<string, int> ResourceCountByName
	{
		get
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (DungeonResource entity in entities)
			{
				dictionary.TryAdd(entity.ResourceName, 0);
				dictionary[entity.ResourceName]++;
			}
			return dictionary;
		}
	}

	public Counter CounterGenerator => counterGenerator;

	public IEnumerable<DungeonResource> AllDungeonResources => entities;

	public DungeonResourceManager()
	{
	}

	[JsonConstructor]
	public DungeonResourceManager(Counter counterGrow, Counter counterGenerator, IndexList<DungeonResource> entities)
	{
		this.entities = entities;
		DungeonResource[] array = this.entities.Where((DungeonResource x) => x.currentHealth <= 0).ToArray();
		foreach (DungeonResource item in array)
		{
			entities.Remove(item);
		}
		this.counterGrow = counterGrow;
		this.counterGenerator = counterGenerator;
	}

	public int _ClearInvalidResource()
	{
		Queue<DungeonResource> queue = new Queue<DungeonResource>(AllDungeonResources.Where((DungeonResource x) => !x.IsValid).ToArray());
		int count = queue.Count;
		while (queue.Count > 0)
		{
			RemoveResource(queue.Dequeue());
		}
		return count;
	}

	public void RefreshCounterInterval()
	{
		SeasonInfo seasonProto = DolocAPI.archiveHandle.timeData.SeasonProto;
		counterGrow.ValidateInterval(seasonProto.ResourceGrowInterval);
		counterGenerator.ValidateInterval(seasonProto.ResourceSpawnInterval);
	}

	public void GrowNatureElements(bool isRender)
	{
		if (!counterGrow.Tick() || entities.IsNullOrEmpty())
		{
			return;
		}
		foreach (DungeonResource entity in entities)
		{
			entity.Grow(useTween: false);
		}
	}

	public DungeonResource TryGetRandomResource(DungeonResourceClass classType)
	{
		return entities.Where((DungeonResource entity) => entity.Proto.ResourceClass == classType).ToArray().Choice();
	}

	public DungeonResource TryGetRandomResource(string id)
	{
		if (id.IsNullOrEmpty())
		{
			return null;
		}
		return entities.Where((DungeonResource entity) => entity.Proto.Id == id).ToArray().Choice();
	}

	public DungeonResource TryGetRandomResource(IEnumerable<string> ids)
	{
		HashSet<string> set = new HashSet<string>(ids);
		if (set.Count == 0)
		{
			return null;
		}
		return entities.Where((DungeonResource entity) => set.Contains(entity.Proto.Id)).ToArray().Choice();
	}

	public DungeonResource TryGetRandomResource(IEnumerable<string> ids, int[] weights)
	{
		HashSet<string> set = new HashSet<string>(ids);
		if (set.Count == 0)
		{
			return null;
		}
		return entities.Where((DungeonResource entity) => set.Contains(entity.Proto.Id)).ToArray().Choice(weights);
	}

	public DungeonResource TryGetRandomResource(IEnumerable<string> ids, float[] weights)
	{
		HashSet<string> set = new HashSet<string>(ids);
		if (set.Count == 0)
		{
			return null;
		}
		return entities.Where((DungeonResource entity) => set.Contains(entity.Proto.Id)).ToArray().Choice(weights);
	}

	public void Clear()
	{
		entities.Clear();
	}

	public DungeonResource CreateResource(IDungeonResourceHost host, ResourceInfo proto, Vector2Int anchor)
	{
		if (!DungeonResourceTypes.TryGetValue(proto.ResourceType, out var value))
		{
			value = Type.GetType(proto.ResourceType_Ref.ClassTypeName);
			DungeonResourceTypes.Add(proto.ResourceType, Type.GetType(proto.ResourceType_Ref.ClassTypeName));
		}
		if (value == null)
		{
			return null;
		}
		Vector3 vector = (new Vector2((float)anchor.x + (float)proto.Width * 0.5f, anchor.y) + host.CurrentRoom.RoomGridPos) * 1.5f;
		DungeonResource dungeonResource = (DungeonResource)Activator.CreateInstance(value, host, proto, vector, anchor);
		entities.Add(dungeonResource);
		dungeonResource.SetMaxHealth();
		return dungeonResource;
	}

	public bool RemoveResource(DungeonResource resource)
	{
		return entities.Remove(resource);
	}
}
