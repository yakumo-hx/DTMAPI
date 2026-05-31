using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace DolocTown.Config.Settings;

public sealed class RebindActionInfo : BeanBase
{
	public const int __ID__ = 1963119487;

	private Dictionary<string, List<int>> bindingIndexCache = new Dictionary<string, List<int>>();

	public string Id { get; private set; }

	public InputActionBinds ActionPath { get; private set; }

	public InputActionBinds[] ActionLinkedPaths { get; private set; }

	public bool AllowEmpty { get; private set; }

	public string[] Labels { get; private set; }

	public bool LockMianInKeyboradMouseMode { get; private set; }

	public bool LockMianInGamepadMode { get; private set; }

	private DolocInputSource actionAsset => DolocAPI.UserInput?.inputSource;

	public InputAction MainAction { get; private set; }

	public List<InputAction> LinkedAction { get; private set; } = new List<InputAction>();


	public bool Valid
	{
		get
		{
			ReadOnlyArray<InputBinding>? readOnlyArray = MainAction?.bindings;
			if (readOnlyArray.HasValue)
			{
				return readOnlyArray.GetValueOrDefault().Count > 0;
			}
			return false;
		}
	}

	private InputBinding.DisplayStringOptions displayStringOptions => InputBinding.DisplayStringOptions.DontIncludeInteractions;

	public RebindActionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["action_path"].IsObject)
		{
			throw new SerializationException();
		}
		ActionPath = InputActionBinds.DeserializeInputActionBinds(_json["action_path"]);
		JSONNode jSONNode = _json["action_linked_paths"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ActionLinkedPaths = new InputActionBinds[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			InputActionBinds inputActionBinds = InputActionBinds.DeserializeInputActionBinds(child);
			ActionLinkedPaths[num++] = inputActionBinds;
		}
		if (!_json["allow_empty"].IsBoolean)
		{
			throw new SerializationException();
		}
		AllowEmpty = _json["allow_empty"];
		JSONNode jSONNode2 = _json["labels"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		Labels = new string[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsString)
			{
				throw new SerializationException();
			}
			string text = child2;
			Labels[num2++] = text;
		}
		if (!_json["lock_mian_in_keyborad_mouse_mode"].IsBoolean)
		{
			throw new SerializationException();
		}
		LockMianInKeyboradMouseMode = _json["lock_mian_in_keyborad_mouse_mode"];
		if (!_json["lock_mian_in_gamepad_mode"].IsBoolean)
		{
			throw new SerializationException();
		}
		LockMianInGamepadMode = _json["lock_mian_in_gamepad_mode"];
	}

	public RebindActionInfo(string id, InputActionBinds action_path, InputActionBinds[] action_linked_paths, bool allow_empty, string[] labels, bool lock_mian_in_keyborad_mouse_mode, bool lock_mian_in_gamepad_mode)
	{
		Id = id;
		ActionPath = action_path;
		ActionLinkedPaths = action_linked_paths;
		AllowEmpty = allow_empty;
		Labels = labels;
		LockMianInKeyboradMouseMode = lock_mian_in_keyborad_mouse_mode;
		LockMianInGamepadMode = lock_mian_in_gamepad_mode;
	}

	public static RebindActionInfo DeserializeRebindActionInfo(JSONNode _json)
	{
		return new RebindActionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1963119487;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ActionPath?.Resolve(_tables);
		InputActionBinds[] actionLinkedPaths = ActionLinkedPaths;
		for (int i = 0; i < actionLinkedPaths.Length; i++)
		{
			actionLinkedPaths[i]?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ActionPath?.TranslateText(translator);
		InputActionBinds[] actionLinkedPaths = ActionLinkedPaths;
		for (int i = 0; i < actionLinkedPaths.Length; i++)
		{
			actionLinkedPaths[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ActionPath:" + ActionPath?.ToString() + ",ActionLinkedPaths:" + StringUtil.CollectionToString(ActionLinkedPaths) + ",AllowEmpty:" + AllowEmpty + ",Labels:" + StringUtil.CollectionToString(Labels) + ",LockMianInKeyboradMouseMode:" + LockMianInKeyboradMouseMode + ",LockMianInGamepadMode:" + LockMianInGamepadMode + ",}";
	}

	private void PostResolve()
	{
		if (actionAsset == null)
		{
			return;
		}
		InputActionBinds actionPath = ActionPath;
		if (actionPath != null && !actionPath.IsEmpty)
		{
			MainAction = actionAsset.FindAction(ActionPath.Path);
			InputActionBinds[] actionLinkedPaths = ActionLinkedPaths;
			foreach (InputActionBinds inputActionBinds in actionLinkedPaths)
			{
				if (!inputActionBinds.IsEmpty)
				{
					InputAction inputAction = actionAsset.FindAction(inputActionBinds.Path);
					if (inputAction != null)
					{
						_ = inputAction.bindings;
						LinkedAction.Add(inputAction);
					}
				}
			}
		}
		string[] names = Enum.GetNames(typeof(InputSchemaType));
		foreach (string text in names)
		{
			List<int> list = new List<int>();
			if (MainAction == null)
			{
				continue;
			}
			for (int j = 0; j < MainAction.bindings.Count; j++)
			{
				InputBinding inputBinding = MainAction.bindings[j];
				string groups = inputBinding.groups;
				if (groups != null && groups.Contains(text) && (!inputBinding.isPartOfComposite || !(inputBinding.name.ToLower() != ActionPath.ComposePartName.ToLower())))
				{
					list.Add(j);
				}
			}
			bindingIndexCache.Add(text, list);
		}
	}

	public List<int> GetBindingIndexes(InputSchemaType inputSchema)
	{
		bindingIndexCache.TryGetValue(inputSchema.ToString(), out var value);
		return value;
	}

	public bool GetFirstControlPath(InputSchemaType inputSchema, out string deviceLayoutName, out string controlPath, out string displayName)
	{
		controlPath = string.Empty;
		deviceLayoutName = string.Empty;
		displayName = string.Empty;
		if (actionAsset == null)
		{
			return false;
		}
		foreach (int bindingIndex in GetBindingIndexes(inputSchema))
		{
			string bindingDisplayString = MainAction.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
			if (!controlPath.IsNullOrEmpty())
			{
				displayName = bindingDisplayString;
				return true;
			}
		}
		return false;
	}
}
