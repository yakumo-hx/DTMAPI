using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public static class NpcTaskRenderControllerExtensions
{
	private static readonly Dictionary<string, Type> _controllerTypeMap = LoadAllNpcTaskRenderControllers();

	private static Dictionary<string, Type> LoadAllNpcTaskRenderControllers()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] subTypes = typeof(NpcTaskRenderController).GetSubTypes();
		foreach (Type type in subTypes)
		{
			object[] customAttributes = type.GetCustomAttributes(typeof(NpcTaskRenderControllerAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				NpcTaskRenderControllerAttribute npcTaskRenderControllerAttribute = (NpcTaskRenderControllerAttribute)customAttributes[0];
				if (!dictionary.TryAdd(npcTaskRenderControllerAttribute.npcName, type))
				{
					Debug.LogWarning("redefined NpcTaskRenderController for npc " + npcTaskRenderControllerAttribute.npcName);
				}
			}
		}
		return dictionary;
	}

	public static NpcTaskRenderController CreateNpcTaskController(this Npc npc)
	{
		Type value;
		NpcTaskRenderController npcTaskRenderController = ((!_controllerTypeMap.TryGetValue(npc.proto.Id, out value)) ? new NpcTaskRenderController() : ((NpcTaskRenderController)Activator.CreateInstance(value)));
		npcTaskRenderController.BindNpc(npc);
		return npcTaskRenderController;
	}
}
