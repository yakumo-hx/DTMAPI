using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CaseLocator : Equipment
{
	private readonly EquipmentFuncCaseLocator func;

	public override bool TouchableAsDecal => false;

	public CaseLocator(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncCaseLocator)proto.Function;
	}

	[JsonConstructor]
	public CaseLocator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			func = (EquipmentFuncCaseLocator)proto.Function;
		}
	}

	public override void SetDecalHost(IDecalHost decalHost)
	{
		base.SetDecalHost(decalHost);
		SetHostSharedState(value: true);
	}

	private void SetHostSharedState(bool value)
	{
		if (base.DecalHost is ILocatable locatable)
		{
			locatable.IsShared = value;
		}
	}

	public override bool HostFilter(IDecalHost host)
	{
		if (base.HostFilter(host))
		{
			return host is ILocatable;
		}
		return false;
	}

	public override void OnCreated()
	{
		base.OnCreated();
		SetHostSharedState(value: true);
	}

	protected override void OnRemove()
	{
		base.OnRemove();
		SetHostSharedState(value: false);
	}

	protected override void OnRender()
	{
		base.OnRender();
		if (func.Lamp_Ref != null)
		{
			base.Renderer.Sr.ToggleLightOn(func.Lamp_Ref.EmissionSpriteAsset.Asset, func.Lamp_Ref.EmissionColor);
		}
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		base.Renderer.Sr.RenderAsNormal();
	}
}
