using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DolocTown.Rendering;

public static class ShaderUtils
{
	public static IEnumerable<string> GetMaterialPropertyIDs(Material mat, ShaderPropertyType type)
	{
		if (mat == null || mat.shader == null)
		{
			yield break;
		}
		Shader shader = mat.shader;
		int propertyCount = shader.GetPropertyCount();
		for (int i = 0; i < propertyCount; i++)
		{
			if (shader.GetPropertyType(i) == type)
			{
				yield return shader.GetPropertyName(i);
			}
		}
	}
}
