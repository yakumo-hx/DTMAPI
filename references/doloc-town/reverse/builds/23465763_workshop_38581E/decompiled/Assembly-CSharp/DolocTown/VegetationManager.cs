using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DolocTown.Config.Resource;
using DolocTown.Config.Time;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class VegetationManager : TerrainDataManager<Vegetation>
{
	private static readonly Dictionary<string, Type> VegetationTypes = LoadVegetationEntityTypes();

	[JsonProperty]
	private readonly Counter counterGrow = new Counter();

	[JsonProperty]
	private readonly Counter counterGenerator = new Counter();

	public Counter CounterGenerator => counterGenerator;

	public Dictionary<string, int> ResourceCount
	{
		get
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (Vegetation allData in base.AllDatas)
			{
				dictionary.TryAdd(allData.VegetationName, 0);
				dictionary[allData.VegetationName]++;
			}
			return dictionary;
		}
	}

	private static Dictionary<string, Type> LoadVegetationEntityTypes()
	{
		Type baseClass = typeof(Vegetation);
		IEnumerable<Type> enumerable = from x in Assembly.GetExecutingAssembly().GetTypes()
			where x.IsSubclassOf(baseClass)
			where !x.IsAbstract
			select x;
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		foreach (Type item in enumerable)
		{
			string key = item.Name.Replace("Vegetation", "VegetationFunc");
			dictionary.Add(key, item);
		}
		return dictionary;
	}

	public VegetationManager()
	{
	}

	[JsonConstructor]
	public VegetationManager(IndexList<Vegetation> datas, Counter counterGrow, Counter counterGenerator)
		: base(datas)
	{
		this.counterGrow = counterGrow ?? new Counter();
		this.counterGenerator = counterGenerator ?? new Counter();
	}

	public Vegetation CreateVegetation(VegetationInfo proto, Vector2Int anchor, Vector3 position)
	{
		string name = proto.Function.GetType().Name;
		if (!VegetationTypes.TryGetValue(name, out var value))
		{
			DolocAPI.outputError("未知的植物类型:" + name);
			return null;
		}
		Vegetation vegetation = (Vegetation)Activator.CreateInstance(value, proto, anchor, position, Grid2D.CoverHArray(anchor, proto.Width));
		AddData(vegetation);
		return vegetation;
	}

	public void UpdatePerTu()
	{
		if (!counterGrow.Tick())
		{
			return;
		}
		foreach (Vegetation allData in base.AllDatas)
		{
			allData.UpdatePerTu();
		}
	}

	public void UpdatePerTuNoRender()
	{
		if (!counterGrow.Tick())
		{
			return;
		}
		foreach (Vegetation allData in base.AllDatas)
		{
			allData.UpdatePerTuNoRender();
		}
	}

	public void RefreshCounterInterval()
	{
		SeasonInfo seasonProto = DolocAPI.archiveHandle.timeData.SeasonProto;
		counterGrow.ValidateInterval(seasonProto.VegetationGrowInterval);
		counterGenerator.ValidateInterval(seasonProto.VegetationSpawnInterval);
	}

	public void UpdateLuminousPlantState()
	{
		foreach (Vegetation allData in base.AllDatas)
		{
			if (allData is ILuminous luminous)
			{
				luminous.OnLightParamChanged(isInitial: true);
			}
		}
	}
}
