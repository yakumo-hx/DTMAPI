using System.Collections.Generic;
using DG.Tweening;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public abstract class Affector : Equipment, IAffector
{
	protected readonly EquipmentFuncAffector func;

	private Sequence shinySequence;

	private HashSet<Vector2Int> _affectedPositions;

	private Vector2Int[] _currentAffectedPositions;

	public abstract AffectorType AffectType { get; }

	public Vector2Int[] CurrentAffectedPositions => _currentAffectedPositions;

	public IEquipmentHost equipmentHost => base.Host;

	protected Affector(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncAffector)proto.Function;
		_currentAffectedPositions = func.GetAffectedPositions(anchor, proto.CoverSize);
	}

	[JsonConstructor]
	protected Affector(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = (EquipmentFuncAffector)proto.Function;
		_currentAffectedPositions = func.GetAffectedPositions(anchor, proto.CoverSize);
	}

	protected void ShineArea(Color color)
	{
		GridArea renderComponent = base.Renderer.GetRenderComponent<GridArea>();
		ResetAreaRender(renderComponent);
		renderComponent.ShineArea(color);
	}

	protected void ResetAreaRender(GridArea areaRender)
	{
		areaRender.GridSize = proto.CoverSize + new Vector2Int(2 * func.HorizontalRange, func.VerticalRangeTop + func.VerticalRangeBottom);
		areaRender.GridPosition = this.GetGlobalAnchor() - new Vector2Int(func.HorizontalRange, func.VerticalRangeBottom);
	}

	public void DoAffector()
	{
		((IAffector)this).InvokeAffect();
	}

	public override void OnMove()
	{
		base.OnMove();
		_currentAffectedPositions = func.GetAffectedPositions(base.Anchor, proto.CoverSize);
	}

	protected override void OnUnRender()
	{
		base.Renderer.RemoveRenderComponent<GridArea>();
	}

	public bool IsCoverPositions(Vector2Int[] positions)
	{
		if (_affectedPositions == null)
		{
			_affectedPositions = new HashSet<Vector2Int>(_currentAffectedPositions);
		}
		return _affectedPositions.Overlaps(positions);
	}
}
