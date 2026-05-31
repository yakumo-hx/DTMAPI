using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown.GameData;

public class EffectsSOInstAnim : EffectsSO
{
	[SerializeField]
	private string _typeName = availableAnimNames.First();

	[SerializeField]
	private Material _material;

	[SerializeField]
	private string _layerName = "Default";

	[SerializeField]
	private int _orderInLayer;

	private static IEnumerable<string> availableAnimNames => Enum.GetNames(typeof(InstAnimEffectType));

	private IEnumerable<string> AvailableLayerNames => SortingLayer.layers.Select((SortingLayer x) => x.name);

	private void OnTypeNameChanged(string value)
	{
		if (value == null)
		{
			_typeName = availableAnimNames.First();
		}
	}

	public override void Raise(Vector2 ws)
	{
		if (Enum.TryParse<InstAnimEffectType>(_typeName, ignoreCase: true, out var result))
		{
			DolocAPI.effectProvider.RaiseInstAnim(ws, result, _material);
		}
	}

	public override void Raise(Vector2 ws, Vector2 dir)
	{
		if (Enum.TryParse<InstAnimEffectType>(_typeName, ignoreCase: true, out var result))
		{
			DolocAPI.effectProvider.RaiseInstAnim(ws, result, dir, flip: false, _material, _layerName, _orderInLayer);
		}
	}
}
