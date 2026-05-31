using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class PositionViewer : DolocUiObject
{
	private Text[] slots;

	[SerializeField]
	public DolocNavigationButton button;

	private MapTipData currentData;

	protected override void __Init()
	{
		base.__Init();
		slots = GetComponentsInChildren<Text>(includeInactive: true);
		button.Init();
		button.onPointerEnter.AddListener(delegate
		{
			button.HoverTextSmall(base.staticTexts.UiTipOpenMap);
		});
		button.onPointerExit.AddListener(delegate
		{
			button.HideHoverBox();
		});
		button.onClick.AddListener(delegate
		{
			if (!currentData.missionId.IsNullOrEmpty())
			{
				DolocAPI.EnterUI((CollectionBookUiState state) => state.OpenMapWithMission(currentData.missionId));
				button.HideHoverBox();
			}
		});
	}

	public void Render(MapTipData data)
	{
		if (!data.notEmpty)
		{
			currentData = default(MapTipData);
			base.gameObject.SetActive(value: false);
			return;
		}
		currentData = data;
		int mapTipCount = data.mapTipCount;
		for (int i = 0; i < slots.Length; i++)
		{
			Text text = slots[i];
			if (i < mapTipCount)
			{
				text.text = data.positionInfos[i];
			}
			text.gameObject.SetActive(i < mapTipCount && !data.positionInfos[i].IsNullOrEmpty());
		}
		base.gameObject.SetActive(slots.Any((Text x) => x.gameObject.activeSelf));
	}
}
