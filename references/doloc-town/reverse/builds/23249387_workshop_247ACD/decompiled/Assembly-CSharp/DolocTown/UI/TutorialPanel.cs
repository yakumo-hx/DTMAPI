using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TutorialPanel : AutoSizeUIPanel
{
	public enum TutorialPanelMode
	{
		Single,
		Multiple
	}

	[SerializeField]
	private Image image;

	[SerializeField]
	public DolocButtonComponent LeftBtn;

	[SerializeField]
	public DolocButtonComponent RightBtn;

	[SerializeField]
	private Text pageInfo;

	[SerializeField]
	private GameObject pageObject;

	[SerializeField]
	private Transform guideRoot;

	protected override void __Init()
	{
		base.__Init();
		image.color = DolocColor.empty;
	}

	public void SetMode(TutorialPanelMode mode)
	{
		switch (mode)
		{
		case TutorialPanelMode.Single:
			pageObject.gameObject.SetActive(value: false);
			break;
		case TutorialPanelMode.Multiple:
			pageObject.gameObject.SetActive(value: true);
			break;
		}
	}

	public bool LoadPage(string prefabName)
	{
		bool flag = false;
		GameObject gameObject = null;
		for (int i = 0; i < guideRoot.childCount; i++)
		{
			gameObject = guideRoot.GetChild(i).gameObject;
			if (gameObject.name == prefabName)
			{
				flag = true;
				gameObject.SetActive(value: true);
			}
			else
			{
				gameObject.SetActive(value: false);
			}
		}
		if (!flag)
		{
			GameObject asset = DolocAPI.assets.cache.GetAsset<GameObject>(prefabName);
			if (asset == null)
			{
				Debug.LogError("预制体加载失败：" + prefabName);
				return false;
			}
			gameObject = Object.Instantiate(asset, guideRoot);
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.name = prefabName;
			gameObject.SetActive(value: true);
		}
		RefreshKeyImage();
		return true;
	}

	public void SetPage(int current, int total)
	{
		pageInfo.text = $"- {current + 1}/{total} -";
	}

	public void RefreshKeyImage()
	{
		IInputDeviceDetect[] componentsInChildren = guideRoot.GetComponentsInChildren<IInputDeviceDetect>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].OnRefresh(DolocAPI.UserInput.DeviceType);
		}
	}
}
