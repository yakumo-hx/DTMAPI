using System.Collections.Generic;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown;

public static class AgentHatRendererUtils
{
	private static readonly Dictionary<string, AgentHatRenderer> _instances = new Dictionary<string, AgentHatRenderer>();

	private static readonly Dictionary<string, AgentHatRenderer> _ridingInstances = new Dictionary<string, AgentHatRenderer>();

	public static bool TryLoadHatRenderer(string hatName, bool isRiding, out AgentHatRenderer hatRenderer)
	{
		hatRenderer = null;
		if (hatName.IsNullOrEmpty())
		{
			return false;
		}
		Dictionary<string, AgentHatRenderer> dictionary = (isRiding ? _ridingInstances : _instances);
		if (dictionary.Count > 10)
		{
			foreach (AgentHatRenderer value in dictionary.Values)
			{
				Object.Destroy(value.GameObject);
			}
			dictionary.Clear();
		}
		if (dictionary.TryGetValue(hatName, out hatRenderer))
		{
			return true;
		}
		hatRenderer = _CreateInstance(hatName);
		if (hatRenderer == null)
		{
			return false;
		}
		dictionary[hatName] = hatRenderer;
		hatRenderer.transform.localPosition = new Vector3(0f, 0f, -0.01f);
		hatRenderer.InitHat();
		if (isRiding)
		{
			hatRenderer.SetAsRiding();
		}
		return true;
	}

	private static AgentHatRenderer _CreateInstance(string hatName)
	{
		if (!DolocConfig.Tables.TbHat.DataMap.TryGetValue(hatName, out var value))
		{
			return null;
		}
		if (value.Prefab.Asset == null)
		{
			return null;
		}
		return Object.Instantiate(value.Prefab.Asset).GetComponent<AgentHatRenderer>();
	}
}
