using DolocTown.Config;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DolocTown.UI;

public class InputActionKeyIcon : DolocUiObject
{
	[SerializeField]
	private Image background;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private Image iconMask;

	[SerializeField]
	private TMP_Text txtKey;

	[SerializeField]
	private Sprite defaultSprite;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private GameObject lockedMask;

	[SerializeField]
	private GameObject lockedIcon;

	[SerializeField]
	private GameObject conflictMask;

	[SerializeField]
	private GameObject conflictIcon;

	private DolocInputDeviceType currentDeviceType;

	private InputAction action;

	private int bindingIndex;

	private InputBinding.DisplayStringOptions displayStringOptions => InputBinding.DisplayStringOptions.DontIncludeInteractions;

	public bool IsLocked
	{
		get
		{
			return lockedMask.activeSelf;
		}
		set
		{
			lockedMask.SetActive(value);
			lockedIcon.SetActive(value);
		}
	}

	public bool IsConflict
	{
		get
		{
			return conflictMask.activeSelf;
		}
		set
		{
			conflictMask.SetActive(value);
			conflictIcon.SetActive(value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		canvasGroup.alpha = 1f;
		canvasGroup.gameObject.SetActive(value: true);
		IsLocked = false;
		IsConflict = false;
	}

	public bool Render(InputAction action, int bindingIndex, DolocInputDeviceType deviceType)
	{
		this.action = action;
		this.bindingIndex = bindingIndex;
		currentDeviceType = deviceType;
		return RefreshView();
	}

	public void Clear()
	{
		IsConflict = false;
		IsLocked = false;
		bindingIndex = -1;
		action = null;
		SetSprite(null);
	}

	private bool RefreshView()
	{
		SetText("");
		InputAction inputAction = action;
		if (inputAction != null)
		{
			_ = inputAction.bindings;
			if (0 == 0 && bindingIndex >= 0 && bindingIndex < action.bindings.Count)
			{
				string deviceLayoutName;
				string controlPath;
				string bindingDisplayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
				return TryRenderKey(currentDeviceType.ToString(), controlPath, bindingDisplayString);
			}
		}
		return false;
	}

	private bool TryRenderKey(string deviceLayoutName, string controlPath, string keyName)
	{
		Sprite sprite = DolocConfig.Tables.TbGameKeyIcon.Get(deviceLayoutName, controlPath)?.LargeIcon.Asset;
		SetSprite(sprite);
		if (sprite == null)
		{
			canvasGroup.alpha = 0.7f;
			return false;
		}
		return true;
	}

	private void SetSprite(Sprite sprite)
	{
		canvasGroup.alpha = 1f;
		if (sprite == null)
		{
			sprite = defaultSprite;
		}
		SetSprite(icon, sprite, autoSize: true);
		SetSprite(background, sprite, autoSize: true);
		iconMask.sprite = sprite;
	}

	public void SetText(string text)
	{
		txtKey.text = text;
	}
}
