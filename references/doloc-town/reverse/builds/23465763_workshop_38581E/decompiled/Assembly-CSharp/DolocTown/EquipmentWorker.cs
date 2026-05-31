using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public abstract class EquipmentWorker : Equipment, IEquipmentWorker
{
	[JsonProperty]
	private bool isIdle;

	[JsonProperty]
	private bool isWorking;

	[JsonProperty]
	private readonly Counter counter;

	protected IEquipmentWorker self;

	protected ElectronicComponentAppliance appliance;

	protected IEquipmentWorkerRenderer workRenderer;

	protected IEquipmentWorkerRenderer extraWorkerRenderer;

	protected IEquipmentWorkerRenderer lowPowerRenderer;

	public virtual int WorkScale => 0;

	[DebugInfo("是否正在工作", Color = "#ff4f4f")]
	public bool IsWorking => isWorking;

	[DebugInfo("是否正在待机", Color = "#ff4f4f")]
	public bool IsIdle => isIdle;

	[DebugInfo("工作记录器", Color = "#ff4f4f")]
	public string CounterInfo => counter.ToString();

	IEquipmentWorkerRenderer IEquipmentWorker.WorkRenderer => workRenderer;

	IEquipmentWorkerRenderer IEquipmentWorker.ExtraWorkerRenderer => extraWorkerRenderer;

	IEquipmentWorkerRenderer IEquipmentWorker.LowPowerWorkerRenderer => lowPowerRenderer;

	Counter IEquipmentWorker.WorkCounter => counter;

	protected Counter WorkCounter => counter;

	bool IEquipmentWorker.IsWorking
	{
		get
		{
			return isWorking;
		}
		set
		{
			isWorking = value;
		}
	}

	bool IEquipmentWorker.IsIdle
	{
		get
		{
			return isIdle;
		}
		set
		{
			isIdle = value;
		}
	}

	protected EquipmentWorker(IEquipmentHost room, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(room, instanceId, proto, worldPos, anchor, turn)
	{
		counter = new Counter();
		isWorking = false;
		isIdle = false;
		InitWorker();
	}

	[JsonConstructor]
	protected EquipmentWorker(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		this.isIdle = isIdle;
		this.isWorking = isWorking;
		this.counter = counter ?? new Counter();
		InitWorker();
	}

	protected void ValidateCounter(int tuCount)
	{
		counter.ValidateInterval(tuCount);
	}

	private void InitWorker()
	{
		self = this;
		if (proto.Function is EquipmentFuncWorker equipmentFuncWorker)
		{
			workRenderer = WorkerRenderer.CreateWorkerRenderer(equipmentFuncWorker.WorkerRendererName.ToString().ToLower(), this);
			extraWorkerRenderer = WorkerRenderer.CreateWorkerRenderer(equipmentFuncWorker.WorkerRendererNameEx.ToString().ToLower(), this);
			lowPowerRenderer = WorkerRenderer.CreateWorkerRenderer(equipmentFuncWorker.WorkerRendererNameLowpower.ToString().ToLower(), this);
		}
		else
		{
			workRenderer = WorkerRenderer.DefaultRenderer;
			extraWorkerRenderer = WorkerRenderer.DefaultRenderer;
			lowPowerRenderer = WorkerRenderer.DefaultRenderer;
		}
		appliance = null;
		if (proto.isElectrical)
		{
			appliance = (ElectronicComponentAppliance)electronicComponent;
		}
	}

	protected sealed override void Update()
	{
		self.UpdateWorker();
	}

	protected sealed override void UpdateNoRender()
	{
		self.UpdateWorkerNoRender();
	}

	protected override void OnRender()
	{
		if (IsWorking)
		{
			if (IsIdle)
			{
				workRenderer.OnIdle();
				extraWorkerRenderer.OnIdle();
				lowPowerRenderer.OnIdle();
			}
			else
			{
				workRenderer.OnWork();
				extraWorkerRenderer.OnWork();
				lowPowerRenderer.OnWork();
			}
		}
		else
		{
			workRenderer.OnStop();
			extraWorkerRenderer.OnStop();
			lowPowerRenderer.OnStop();
		}
	}

	protected override void OnUnRender()
	{
		workRenderer?.OnRemove();
		extraWorkerRenderer?.OnRemove();
		lowPowerRenderer?.OnRemove();
	}

	protected void Work(int n)
	{
		self.Work(n * DolocAPI.GlobalParameter.TULength);
	}

	protected void WorkNoRender(int n)
	{
		self.WorkNoRender(n * DolocAPI.GlobalParameter.TULength);
	}

	protected void ForceWork(int n)
	{
		self.ForceWork(n * DolocAPI.GlobalParameter.TULength);
	}

	protected void ForceWorkNoRender(int n)
	{
		self.ForceWorkNoRender(n * DolocAPI.GlobalParameter.TULength);
	}

	public virtual void OnWorking(bool isRender)
	{
	}

	protected virtual void OnWorkDone()
	{
	}

	protected virtual void OnWorkDoneNoRender()
	{
	}

	bool IEquipmentWorker.Launch()
	{
		if (proto.isElectrical)
		{
			return appliance.Launch();
		}
		return true;
	}

	public virtual void FinishWork()
	{
		workRenderer.OnWorkDone();
		extraWorkerRenderer.OnWorkDone();
		lowPowerRenderer.OnWorkDone();
		OnWorkDone();
	}

	public virtual void FinishWorkNoRender()
	{
		OnWorkDoneNoRender();
	}

	public virtual void ForceStopCurrentWork()
	{
		self.ForceStopWork();
	}
}
