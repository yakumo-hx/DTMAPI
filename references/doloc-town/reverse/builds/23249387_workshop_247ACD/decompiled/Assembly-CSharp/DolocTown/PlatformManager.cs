using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Platform;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class PlatformManager
{
	[JsonProperty]
	private IndexCounter counter = new IndexCounter();

	private Dictionary<int, List<Platform>> platforms = new Dictionary<int, List<Platform>>();

	private Dictionary<int, List<Vector2Int>> colliders = new Dictionary<int, List<Vector2Int>>();

	public int PlatformCount => totalPlatforms.Count();

	[JsonProperty("platforms")]
	private Platform[] platformsArray => totalPlatforms.ToArray();

	public IEnumerable<Platform> totalPlatforms
	{
		get
		{
			foreach (List<Platform> value in platforms.Values)
			{
				foreach (Platform item in value)
				{
					yield return item;
				}
			}
		}
	}

	public IEnumerable<Vector3Int> totalColliders
	{
		get
		{
			foreach (KeyValuePair<int, List<Vector2Int>> kv in colliders)
			{
				foreach (Vector2Int item in kv.Value)
				{
					yield return new Vector3Int(item.x, item.y, kv.Key);
				}
			}
		}
	}

	public PlatformManager()
	{
	}

	[JsonConstructor]
	private PlatformManager(IndexCounter counter, Platform[] platforms)
	{
		this.counter = counter;
		if (this.platforms == null)
		{
			return;
		}
		foreach (Platform platform in platforms)
		{
			if (!platform.isDeserializationValid)
			{
				counter.Remove(platform.id);
				continue;
			}
			PlatformGeometry geometry = platform.geometry;
			this.platforms.TryAdd(geometry.Height, new List<Platform>());
			this.platforms[geometry.Height].Add(platform);
		}
		UpdateAllColliderInfos();
	}

	public IEnumerable<Vector3Int> GetCollidersAtRow(int height)
	{
		if (!colliders.TryGetValue(height, out var value))
		{
			yield break;
		}
		foreach (Vector2Int item in value)
		{
			yield return new Vector3Int(item.x, item.y, height);
		}
	}

	public Platform CreatePlatform(PlatformGeometry geometry, PlatformInfo proto)
	{
		Platform platform = new Platform(counter.NextIndex, proto, geometry);
		if (!platforms.ContainsKey(geometry.Height))
		{
			platforms.Add(geometry.Height, new List<Platform>());
		}
		platforms[geometry.Height].Add(platform);
		colliders[geometry.Height] = CalculateColliders(platforms[geometry.Height]);
		return platform;
	}

	public void RemovePlatform(Platform pt)
	{
		platforms[pt.Height].Remove(pt);
		counter.Remove(pt.id);
		colliders[pt.Height].Clear();
		colliders[pt.Height] = CalculateColliders(platforms[pt.Height]);
	}

	public Platform TryGetPlatformDiffProto(PlatformGeometry geometry, PlatformInfo proto)
	{
		if (!platforms.TryGetValue(geometry.Height, out var value))
		{
			return null;
		}
		return value.FirstOrDefault((Platform pt) => pt.geometry.IsSame(geometry) && pt.proto != proto);
	}

	private List<Vector2Int> CalculateColliders(List<Platform> platforms)
	{
		platforms.Sort();
		List<Vector2Int> list = new List<Vector2Int>();
		List<Platform> list2 = new List<Platform>();
		for (int i = 0; i < platforms.Count; i++)
		{
			list2.Add(platforms[i]);
			int num = i + 1;
			if (num < platforms.Count)
			{
				if (platforms[i].geometry.IsNotConnected(platforms[num].geometry))
				{
					list.Add(_Combine(list2));
					list2.Clear();
				}
			}
			else
			{
				list.Add(_Combine(list2));
			}
		}
		return list;
	}

	private Vector2Int _Combine(List<Platform> platforms)
	{
		if (platforms.Count == 1)
		{
			Platform platform = platforms[0];
			return new Vector2Int(platform.geometry.Left, platform.geometry.Right);
		}
		Platform platform2 = platforms[0];
		return new Vector2Int(y: platforms[^1].geometry.Right, x: platform2.geometry.Left);
	}

	public void UpdateAllColliderInfos()
	{
		colliders.Clear();
		foreach (var (key, list2) in platforms)
		{
			colliders[key] = CalculateColliders(list2);
		}
	}

	public PlatformCutInfos CalculateCutInfos(PlatformGeometry ptInfo)
	{
		Dictionary<Platform, List<Vector2Int>> dictionary = new Dictionary<Platform, List<Vector2Int>>();
		foreach (Vector2Int item in ptInfo.PlatformPositionsIncludeBorder)
		{
			foreach (Platform totalPlatform in totalPlatforms)
			{
				if (totalPlatform.geometry.IsColumn(item))
				{
					dictionary.TryAdd(totalPlatform, new List<Vector2Int>());
					dictionary[totalPlatform].Add(item);
					break;
				}
			}
		}
		return new PlatformCutInfos(dictionary);
	}
}
