using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Serialization;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotFunctionFarming : AutomateBotFunction
{
	private struct AreaRect
	{
		private readonly Vector2Int size;

		private readonly Vector2Int anchor;

		public AreaRect(Vector2Int size, Vector2Int anchor)
		{
			this.size = size;
			this.anchor = anchor;
		}
	}

	public const int __ID__ = -1933222533;

	private readonly Dictionary<Vector2Int, Vector2Int[]> SprinklerRelativePositions = new Dictionary<Vector2Int, Vector2Int[]>();

	private readonly Dictionary<AreaRect, Vector2Int[]> SprinklerAbsolutePositionsCache = new Dictionary<AreaRect, Vector2Int[]>();

	private const int MAX_CACHE_SIZE = 64;

	public int SprinklerHorizontalRange { get; private set; }

	public int SprinklerVerticalRangeTop { get; private set; }

	public int SprinklerVerticalRangeBottom { get; private set; }

	public int SprinklerCost { get; private set; }

	public AutomateBotFunctionFarming(JSONNode _json)
		: base(_json)
	{
		if (!_json["sprinkler_horizontal_range"].IsNumber)
		{
			throw new SerializationException();
		}
		SprinklerHorizontalRange = _json["sprinkler_horizontal_range"];
		if (!_json["sprinkler_vertical_range_top"].IsNumber)
		{
			throw new SerializationException();
		}
		SprinklerVerticalRangeTop = _json["sprinkler_vertical_range_top"];
		if (!_json["sprinkler_vertical_range_bottom"].IsNumber)
		{
			throw new SerializationException();
		}
		SprinklerVerticalRangeBottom = _json["sprinkler_vertical_range_bottom"];
		if (!_json["sprinkler_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		SprinklerCost = _json["sprinkler_cost"];
	}

	public AutomateBotFunctionFarming(int sprinkler_horizontal_range, int sprinkler_vertical_range_top, int sprinkler_vertical_range_bottom, int sprinkler_cost)
	{
		SprinklerHorizontalRange = sprinkler_horizontal_range;
		SprinklerVerticalRangeTop = sprinkler_vertical_range_top;
		SprinklerVerticalRangeBottom = sprinkler_vertical_range_bottom;
		SprinklerCost = sprinkler_cost;
	}

	public static AutomateBotFunctionFarming DeserializeAutomateBotFunctionFarming(JSONNode _json)
	{
		return new AutomateBotFunctionFarming(_json);
	}

	public override int GetTypeId()
	{
		return -1933222533;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SprinklerHorizontalRange:" + SprinklerHorizontalRange + ",SprinklerVerticalRangeTop:" + SprinklerVerticalRangeTop + ",SprinklerVerticalRangeBottom:" + SprinklerVerticalRangeBottom + ",SprinklerCost:" + SprinklerCost + ",}";
	}

	public Vector2Int[] GetRange(Vector2Int size, Vector2Int anchor)
	{
		AreaRect key = new AreaRect(size, anchor);
		if (SprinklerAbsolutePositionsCache.TryGetValue(key, out var value))
		{
			return value;
		}
		if (SprinklerAbsolutePositionsCache.Count > 64)
		{
			SprinklerAbsolutePositionsCache.Clear();
		}
		value = GetRelativeRange(size).Offset(anchor).ToArray();
		SprinklerAbsolutePositionsCache[key] = value;
		return value;
	}

	public Vector2Int[] GetRelativeRange(Vector2Int size)
	{
		if (!SprinklerRelativePositions.ContainsKey(size))
		{
			SprinklerRelativePositions[size] = BuildRange(size);
		}
		return SprinklerRelativePositions[size];
	}

	private Vector2Int[] BuildRange(Vector2Int size)
	{
		int num = -SprinklerHorizontalRange;
		int num2 = -SprinklerVerticalRangeBottom;
		int num3 = SprinklerHorizontalRange * 2 + size.x + num;
		int num4 = SprinklerVerticalRangeBottom + SprinklerVerticalRangeTop + size.y + num2;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = num; i < num3; i++)
		{
			for (int j = num2; j < num4; j++)
			{
				list.Add(new Vector2Int(i, j));
			}
		}
		return list.ToArray();
	}
}
