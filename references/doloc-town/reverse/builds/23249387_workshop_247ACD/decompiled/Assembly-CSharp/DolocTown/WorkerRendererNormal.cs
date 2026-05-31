using DolocTown.Config.Equipment;
using UnityEngine;

namespace DolocTown;

[WorkerRenderer("common")]
public class WorkerRendererNormal : WorkerRenderer
{
	private readonly Sprite idleSprite;

	private readonly Sprite normalSprite;

	public WorkerRendererNormal(Equipment equipment)
		: base(equipment)
	{
		if (!(equipment.proto.Function is EquipmentFuncWorker equipmentFuncWorker))
		{
			idleSprite = equipment.proto.Sprite;
			normalSprite = equipment.proto.Sprite;
		}
		else
		{
			idleSprite = equipmentFuncWorker.IdleSprite.Asset ?? equipment.proto.Sprite;
			normalSprite = equipment.proto.Sprite;
		}
	}

	private Sprite GetWorkSprite()
	{
		Sprite sprite = null;
		if (equipment is IEquipmentWorker equipmentWorker && equipment.proto.Function is EquipmentFuncWorker equipmentFuncWorker)
		{
			sprite = equipmentFuncWorker.GetWorkSprite(equipmentWorker.WorkScale);
		}
		if (sprite == null)
		{
			sprite = equipment.proto.Sprite;
		}
		return sprite;
	}

	public override void OnFailed()
	{
	}

	public override void OnIdle()
	{
		base.EquipmentRenderer.Sprite = idleSprite;
	}

	public override void OnStop()
	{
		base.EquipmentRenderer.Sprite = normalSprite;
	}

	public override void OnRemove()
	{
	}

	public override void OnWork()
	{
		base.EquipmentRenderer.Sprite = GetWorkSprite();
	}

	public override void OnWorkDone()
	{
	}
}
