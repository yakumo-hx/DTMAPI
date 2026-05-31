using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct BackgroundLayer
{
	[SerializeField]
	public Sprite sprite;

	[SerializeField]
	public float moveSpeedX;

	[SerializeField]
	public float offsetX;

	[SerializeField]
	public float fixedOffsetX;

	[SerializeField]
	public float moveSpeedY;

	[SerializeField]
	public float saturation;

	[SerializeField]
	public float brightness;

	[SerializeField]
	public Color color;

	[SerializeField]
	public string sortingLayer;

	[SerializeField]
	public int orderInLayer;

	[SerializeField]
	public bool hasAlpha;

	[SerializeField]
	[Range(0f, 1f)]
	public float alpha;

	private IEnumerable<string> AvailableSortingLayers => SortingLayer.layers.Select((SortingLayer x) => x.name);
}
