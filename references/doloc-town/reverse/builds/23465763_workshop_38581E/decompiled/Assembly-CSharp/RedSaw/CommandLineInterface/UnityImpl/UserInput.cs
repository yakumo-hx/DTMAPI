using UnityEngine.InputSystem;

namespace RedSaw.CommandLineInterface.UnityImpl;

public class UserInput : IConsoleInput
{
	public bool Focus
	{
		get
		{
			if (Keyboard.current.ctrlKey.isPressed)
			{
				return Keyboard.current.cKey.wasPressedThisFrame;
			}
			return false;
		}
	}

	public bool MoveUp => Keyboard.current.upArrowKey.wasPressedThisFrame;

	public bool MoveUpC => Keyboard.current.upArrowKey.isPressed;

	public bool MoveDown => Keyboard.current.downArrowKey.wasPressedThisFrame;

	public bool MoveDownC => Keyboard.current.downArrowKey.isPressed;

	public bool QuitFocus => Keyboard.current.escapeKey.wasPressedThisFrame;

	public bool ShowOrHide => Keyboard.current.f1Key.wasPressedThisFrame;
}
