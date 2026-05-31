using System;
using System.Collections.Generic;
using UnityEngine;

namespace XLua;

public static class SysGenConfig
{
	[GCOptimize(OptimizeFlag.Default)]
	private static List<Type> GCOptimize => new List<Type>
	{
		typeof(Vector2),
		typeof(Vector3),
		typeof(Vector4),
		typeof(Color),
		typeof(Quaternion),
		typeof(Ray),
		typeof(Bounds),
		typeof(Ray2D)
	};

	[AdditionalProperties]
	private static Dictionary<Type, List<string>> AdditionalProperties => new Dictionary<Type, List<string>>
	{
		{
			typeof(Ray),
			new List<string> { "origin", "direction" }
		},
		{
			typeof(Ray2D),
			new List<string> { "origin", "direction" }
		},
		{
			typeof(Bounds),
			new List<string> { "center", "extents" }
		}
	};
}
