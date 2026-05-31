using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class ModFunctionButton : DolocNavigationButton
{
	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private string spriteName;

	[SerializeField]
	private Transform arrow;

	public string buttonText
	{
		set
		{
			text.text = value;
			if (text != null)
			{
				text.text = (spriteName.IsNullOrEmpty() ? value : $"{text}<sprite name={spriteName}>");
			}
		}
	}

	protected override void OnGrayed(bool value)
	{
		base.OnGrayed(value);
		base.blocksRaycasts = !value;
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		arrow.gameObject.SetActive(value: true);
	}

	protected override void OnDeselect()
	{
		base.OnDeselect();
		arrow.gameObject.SetActive(value: false);
	}

	private void OnDisable()
	{
		arrow.gameObject.SetActive(value: false);
	}
}
