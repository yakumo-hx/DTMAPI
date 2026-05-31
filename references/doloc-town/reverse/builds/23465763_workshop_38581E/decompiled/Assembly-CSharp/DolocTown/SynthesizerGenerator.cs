using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class SynthesizerGenerator : EquipmentWorker
{
	private readonly EquipmentFuncSynthesizerGenerator func;

	public SynthesizerGenerator(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncSynthesizerGenerator)proto.Function;
	}

	[JsonConstructor]
	protected SynthesizerGenerator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		func = (EquipmentFuncSynthesizerGenerator)proto.Function;
		ValidateCounter(func.Interval);
	}

	public override void OnCreated()
	{
		Work(func.Interval);
	}

	protected override void OnWorkDoneNoRender()
	{
		this.CreateDropItem(func.OutputItemName, shouldRender: false, sendMessage: true);
		Work(func.Interval);
	}

	protected override void OnWorkDone()
	{
		this.CreateDropItem(func.OutputItemName, shouldRender: true, sendMessage: true);
		Work(func.Interval);
	}
}
