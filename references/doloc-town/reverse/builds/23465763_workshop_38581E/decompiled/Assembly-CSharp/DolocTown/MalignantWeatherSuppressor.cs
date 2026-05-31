using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MalignantWeatherSuppressor : EquipmentWorker
{
	private readonly EquipmentFuncMalignantWeatherSuppressor func;

	public int MalignantWeatherSuppressValue
	{
		get
		{
			if (base.IsIdle)
			{
				return 0;
			}
			return 1;
		}
	}

	public MalignantWeatherSuppressor(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncMalignantWeatherSuppressor)proto.Function;
	}

	[JsonConstructor]
	protected MalignantWeatherSuppressor(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		func = (EquipmentFuncMalignantWeatherSuppressor)proto.Function;
	}

	public override void OnCreated()
	{
		Work(func.Duration);
	}

	protected override void OnWorkDone()
	{
		Work(func.Duration);
	}

	protected override void OnWorkDoneNoRender()
	{
		WorkNoRender(func.Duration);
	}
}
