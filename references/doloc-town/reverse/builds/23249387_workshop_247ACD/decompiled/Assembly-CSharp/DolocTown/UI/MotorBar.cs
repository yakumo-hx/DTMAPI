using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MotorBar : DolocUiObject
{
	[SerializeField]
	private Text motorName;

	[SerializeField]
	private Text motorPlace;

	[SerializeField]
	private Text emptyHint;

	[SerializeField]
	private GameObject content;

	[SerializeField]
	public Image motorPreview;

	public void Render(bool unlock, string place)
	{
		content.SetActive(unlock);
		emptyHint.gameObject.SetActive(!unlock);
		motorName.text = base.staticTexts.EquipmentBarMotorTitle;
		motorPlace.text = place;
		emptyHint.text = DolocConfig.StaticTexts.EquipmentBarMotorLock;
		motorPreview.sprite = DolocAPI.GlobalParameter.DefaultVehiclePreview.Asset;
	}
}
