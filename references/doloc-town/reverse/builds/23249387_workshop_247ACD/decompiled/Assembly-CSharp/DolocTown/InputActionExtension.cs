using DolocTown.Config;
using DolocTown.Config.UI;
using UnityEngine.InputSystem;

namespace DolocTown;

public static class InputActionExtension
{
	private static InputBinding.DisplayStringOptions displayStringOptions => InputBinding.DisplayStringOptions.DontIncludeInteractions;

	public static void GetIconGroup(this InputAction action, int bindingIndex, out ActionIconGroup actionIconGroup)
	{
		actionIconGroup = default(ActionIconGroup);
		string deviceLayoutName;
		string controlPath;
		string bindingDisplayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
		GameKeyIconInfo gameKeyIconInfo = DolocConfig.Tables.TbGameKeyIcon.Get(deviceLayoutName, controlPath);
		actionIconGroup = new ActionIconGroup(gameKeyIconInfo?.DefaultDisplayName ?? bindingDisplayString, gameKeyIconInfo?.LargeIcon, gameKeyIconInfo?.SmallIcon);
	}

	public static bool IsJoystick(this DolocInputDeviceType type)
	{
		if (type != DolocInputDeviceType.GamePad && type != DolocInputDeviceType.XboxController && type != DolocInputDeviceType.SwitchProController)
		{
			return type == DolocInputDeviceType.PlayStationController;
		}
		return true;
	}

	public static InputSchemaType GetInputSchema(this DolocInputDeviceType deviceType)
	{
		return deviceType switch
		{
			DolocInputDeviceType.KeyboardMouse => InputSchemaType.KeyboardMouse, 
			DolocInputDeviceType.GamePad => InputSchemaType.GamePad, 
			DolocInputDeviceType.XboxController => InputSchemaType.GamePad, 
			DolocInputDeviceType.SwitchProController => InputSchemaType.GamePad, 
			DolocInputDeviceType.PlayStationController => InputSchemaType.GamePad, 
			_ => InputSchemaType.Other, 
		};
	}
}
