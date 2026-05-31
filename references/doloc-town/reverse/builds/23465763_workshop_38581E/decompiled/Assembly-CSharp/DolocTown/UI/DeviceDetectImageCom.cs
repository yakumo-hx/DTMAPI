using DolocTown.Config;
using DolocTown.Config.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class DeviceDetectImageCom : DolocUiObject, IInputDeviceDetect
{
	public string actionName;

	public bool UseSmallIcon = true;

	[SerializeField]
	private bool autoRefresh;

	private Image image;

	private GameKeyActionInfo _config;

	private Image Image
	{
		get
		{
			if (image == null)
			{
				image = GetComponent<Image>();
			}
			return image;
		}
	}

	public GameKeyActionInfo Config
	{
		get
		{
			if (_config == null)
			{
				_config = DolocConfig.Tables.TbGameKeyAction.GetOrDefault(actionName);
			}
			return _config;
		}
	}

	private void OnEnable()
	{
		if (autoRefresh && DolocAPI.IsGameInitialized)
		{
			OnRefresh(DolocAPI.UserInput.DeviceType);
		}
	}

	public void OnRefresh(DolocInputDeviceType deviceType)
	{
		if (Config == null || !Config.GetIconGroup(deviceType, out var group))
		{
			DolocUtils.setAlpha(Image, 0f);
			return;
		}
		Sprite sprite = (UseSmallIcon ? group.smallIcon : group.largeIcon);
		if (sprite == null)
		{
			DolocUtils.setAlpha(Image, 0f);
			return;
		}
		DolocUtils.setAlpha(Image, 1f);
		SetSprite(Image, sprite, autoSize: true);
	}
}
