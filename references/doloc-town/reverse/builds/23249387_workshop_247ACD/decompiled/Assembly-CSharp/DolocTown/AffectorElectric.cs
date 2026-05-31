using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public abstract class AffectorElectric : Affector
{
	private readonly ElectronicComponentAppliance appliance;

	[JsonProperty]
	[DebugInfo("是否正在待机")]
	private bool isIdle;

	[JsonProperty]
	[DebugInfo("工作计数器")]
	private readonly Counter workCounter;

	[JsonProperty]
	[DebugInfo("是否打开了")]
	private bool isTurnOn;

	public bool IsWorking => !isIdle;

	public bool IsTurnOn => isTurnOn;

	public Vector2 PositionSwitch
	{
		get
		{
			Vector2 worldSpriteSize = proto.WorldSpriteSize;
			return new Vector2(base.Position.x + worldSpriteSize.x * 0.5f, base.Position.y + worldSpriteSize.y);
		}
	}

	public override Vector3 PositionTip => base.PositionBottom + new Vector3(0f, proto.WorldSpriteSize.y + 1.5f, 0f);

	private string KeyPrompt
	{
		get
		{
			if (!isTurnOn)
			{
				return DolocConfig.StaticTexts.UiOperationStart;
			}
			return DolocConfig.StaticTexts.UiOperationClose;
		}
	}

	protected AffectorElectric(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		appliance = (ElectronicComponentAppliance)electronicComponent;
		workCounter = new Counter(func.WorkInterval * DolocAPI.GlobalParameter.TULength);
		isIdle = true;
		isTurnOn = true;
	}

	[JsonConstructor]
	protected AffectorElectric(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, Counter workCounter, bool isIdle, bool isTurnOn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		appliance = (ElectronicComponentAppliance)electronicComponent;
		this.workCounter = workCounter;
		this.isIdle = isIdle;
		this.isTurnOn = isTurnOn;
	}

	protected virtual void OnInvokeAffect()
	{
	}

	protected virtual void OnStopAffect()
	{
	}

	protected override void OnRender()
	{
		base.OnRender();
		if (isTurnOn && isIdle)
		{
			RenderAsLowPower();
		}
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		this.HideSceneOperationTip();
		base.Renderer.RemoveRenderComponent<EquipmentStateRenderer>();
		base.Renderer.RemoveUiComponent<SingleImage>();
	}

	private void UpdateNoInterval()
	{
		if (!isTurnOn)
		{
			return;
		}
		if (isIdle)
		{
			if (!appliance.Launch())
			{
				RenderAsLowPower();
				OnStopAffect();
				return;
			}
			isIdle = false;
			OnInvokeAffect();
			workCounter.Reset();
			base.Renderer.RemoveRenderComponent<EquipmentStateRenderer>();
		}
		else if (workCounter.Tick())
		{
			((IAffector)this).InvokeAffect();
			isIdle = true;
		}
	}

	private void UpdateNoIntervalNoRender()
	{
		if (!isTurnOn)
		{
			return;
		}
		if (isIdle)
		{
			if (appliance.Launch())
			{
				isIdle = false;
				workCounter.Reset();
			}
		}
		else if (workCounter.Tick())
		{
			((IAffector)this).InvokeAffectNoRender();
			isIdle = true;
		}
	}

	protected override void Update()
	{
		if (!isTurnOn)
		{
			return;
		}
		if (isIdle)
		{
			if (appliance.Launch())
			{
				isIdle = false;
				OnInvokeAffect();
				workCounter.Reset();
				base.Renderer.RemoveRenderComponent<EquipmentStateRenderer>();
				((IAffector)this).InvokeAffect();
			}
			else
			{
				RenderAsLowPower();
				OnStopAffect();
			}
		}
		else if (workCounter.Tick())
		{
			isIdle = true;
		}
	}

	protected override void UpdateNoRender()
	{
		if (!isTurnOn)
		{
			return;
		}
		if (isIdle)
		{
			if (appliance.Launch())
			{
				isIdle = false;
				workCounter.Reset();
				((IAffector)this).InvokeAffectNoRender();
			}
		}
		else if (workCounter.Tick())
		{
			isIdle = true;
		}
	}

	protected override void OnTouch()
	{
		UpdateSwitchIcon();
		this.ShowSceneOperationTip(PositionTip, KeyPrompt, DolocAPI.UserInput.GlobalInteractActionName);
	}

	protected override void OnDisTouch()
	{
		if (!(base.Renderer == null))
		{
			base.Renderer.RemoveUiComponent<SingleImage>();
			this.HideSceneOperationTip();
		}
	}

	protected override void OnInteract()
	{
		this.PushSceneOperationTip();
		DolocAPI.agent._Interact(delegate
		{
			isTurnOn = !isTurnOn;
			OnSwitch();
		});
	}

	private void RenderAsLowPower()
	{
		EquipmentStateRenderer renderComponent = base.Renderer.GetRenderComponent<EquipmentStateRenderer>();
		renderComponent.position2d = base.PositionCenter;
		renderComponent.SetAsLowPower();
	}

	private void UpdateSwitchIcon()
	{
		if (!base.Renderer.TryGetFirstUiComponent<SingleImage>(out var component))
		{
			component = base.Renderer.GetUiComponent<SingleImage>();
		}
		component.FollowWorldPosition(PositionSwitch);
		component.Color = Color.white;
		component.SetSprite(isTurnOn ? LocSprites.UI_TIP_FILLEDSOCK : LocSprites.UI_TIP_EMPTYSOCK);
	}

	private void OnSwitch()
	{
		UpdateSwitchIcon();
		ShineArea(isTurnOn ? Color.white : DolocColor.yellow);
		this.ShowSceneOperationTip(PositionTip, KeyPrompt, DolocAPI.UserInput.GlobalInteractActionName);
		if (isTurnOn)
		{
			if (appliance.Launch())
			{
				isIdle = false;
				OnInvokeAffect();
				workCounter.Reset();
				base.Renderer.RemoveRenderComponent<EquipmentStateRenderer>();
				((IAffector)this).InvokeAffect();
			}
			else
			{
				RenderAsLowPower();
			}
		}
		else
		{
			isIdle = true;
			base.Renderer.RemoveRenderComponent<EquipmentStateRenderer>();
			OnStopAffect();
		}
		RenderOnSwitch(isTurnOn);
	}

	protected virtual void RenderOnSwitch(bool value)
	{
	}
}
