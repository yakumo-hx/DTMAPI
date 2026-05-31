using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Settings;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class GameKeyActionInfo : BeanBase
{
	public const int __ID__ = 1167517687;

	public string ActionName { get; private set; }

	public string[] KeyboradMouseActions { get; private set; }

	public RebindActionInfo[] KeyboradMouseActions_Ref { get; private set; }

	public string[] GamepadActions { get; private set; }

	public RebindActionInfo[] GamepadActions_Ref { get; private set; }

	public bool EnableCombined { get; private set; }

	public GameKeyActionInfo(JSONNode _json)
	{
		if (!_json["actionName"].IsString)
		{
			throw new SerializationException();
		}
		ActionName = _json["actionName"];
		JSONNode jSONNode = _json["keyborad_mouse_actions"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		KeyboradMouseActions = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			KeyboradMouseActions[num++] = text;
		}
		JSONNode jSONNode2 = _json["gamepad_actions"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		GamepadActions = new string[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsString)
			{
				throw new SerializationException();
			}
			string text2 = child2;
			GamepadActions[num2++] = text2;
		}
		if (!_json["enable_combined"].IsBoolean)
		{
			throw new SerializationException();
		}
		EnableCombined = _json["enable_combined"];
	}

	public GameKeyActionInfo(string actionName, string[] keyborad_mouse_actions, string[] gamepad_actions, bool enable_combined)
	{
		ActionName = actionName;
		KeyboradMouseActions = keyborad_mouse_actions;
		GamepadActions = gamepad_actions;
		EnableCombined = enable_combined;
	}

	public static GameKeyActionInfo DeserializeGameKeyActionInfo(JSONNode _json)
	{
		return new GameKeyActionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1167517687;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		int num = KeyboradMouseActions.Length;
		TbRebindAction tbRebindAction = (TbRebindAction)_tables["Settings.TbRebindAction"];
		KeyboradMouseActions_Ref = new RebindActionInfo[num];
		for (int i = 0; i < num; i++)
		{
			KeyboradMouseActions_Ref[i] = tbRebindAction.GetOrDefault(KeyboradMouseActions[i]);
		}
		int num2 = GamepadActions.Length;
		TbRebindAction tbRebindAction2 = (TbRebindAction)_tables["Settings.TbRebindAction"];
		GamepadActions_Ref = new RebindActionInfo[num2];
		for (int j = 0; j < num2; j++)
		{
			GamepadActions_Ref[j] = tbRebindAction2.GetOrDefault(GamepadActions[j]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ ActionName:" + ActionName + ",KeyboradMouseActions:" + StringUtil.CollectionToString(KeyboradMouseActions) + ",GamepadActions:" + StringUtil.CollectionToString(GamepadActions) + ",EnableCombined:" + EnableCombined + ",}";
	}

	public bool GetIconGroup(DolocInputDeviceType deviceType, out ActionIconGroup group)
	{
		List<ActionIconGroup> iconGroups;
		bool allIconsGroup = GetAllIconsGroup(deviceType, out iconGroups);
		group = (allIconsGroup ? iconGroups.First() : default(ActionIconGroup));
		return allIconsGroup;
	}

	public bool GetAllIconsGroup(DolocInputDeviceType deviceType, out List<ActionIconGroup> iconGroups)
	{
		InputSchemaType inputSchema = deviceType.GetInputSchema();
		iconGroups = new List<ActionIconGroup>();
		RebindActionInfo[] array;
		switch (inputSchema)
		{
		case InputSchemaType.Other:
			return false;
		case InputSchemaType.GamePad:
			array = GamepadActions_Ref;
			break;
		default:
			array = KeyboradMouseActions_Ref;
			break;
		}
		RebindActionInfo[] array2 = array;
		if (array2.IsNullOrEmpty())
		{
			return false;
		}
		List<(string, string)> list = new List<(string, string)>();
		array = array2;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].GetFirstControlPath(inputSchema, out var _, out var controlPath, out var displayName))
			{
				list.Add((controlPath, displayName));
			}
		}
		if (list.IsNullOrEmpty())
		{
			return false;
		}
		foreach (var item in list)
		{
			GameKeyIconInfo gameKeyIconInfo = DolocConfig.Tables.TbGameKeyIcon.Get(deviceType.ToString(), item.Item1);
			if (gameKeyIconInfo != null)
			{
				iconGroups.Add(new ActionIconGroup(item.Item2, gameKeyIconInfo.LargeIcon, gameKeyIconInfo.SmallIcon));
			}
		}
		return !iconGroups.IsNullOrEmpty();
	}
}
