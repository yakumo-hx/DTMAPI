using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class GameDataSlot : DolocNavigationButton
{
	[SerializeField]
	private Text indexTxt;

	[SerializeField]
	private Text playerNameTxt;

	[SerializeField]
	private Text emptyTxt;

	[SerializeField]
	private CanvasGroup contentCanvasGroup;

	[SerializeField]
	private Text positionTxt;

	[SerializeField]
	private Text gameDateTxt;

	[SerializeField]
	private Text moneyTxt;

	[SerializeField]
	private Text timeSpanTxt;

	[SerializeField]
	private Text realTimeTxt;

	[SerializeField]
	private Color normalTxtColor;

	[SerializeField]
	private Color grayedTxtColor;

	private Color normalBgColor;

	private Color highLightedBgColor;

	protected override void __Init()
	{
		base.__Init();
		emptyTxt.gameObject.SetActive(value: false);
		normalBgColor = base.button.colors.normalColor;
		highLightedBgColor = base.button.colors.selectedColor;
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		this.GetItemBorder(BorderType.Arrow);
		DolocAPI.UIRaiseRoll();
	}

	protected override void OnGrayed(bool value)
	{
		emptyTxt.gameObject.SetActive(value);
		contentCanvasGroup.alpha = (value ? 0f : 1f);
		indexTxt.color = (value ? grayedTxtColor : normalTxtColor);
	}

	protected override void OnHighLighted(bool value)
	{
		ColorBlock colors = base.button.colors;
		colors.normalColor = (value ? highLightedBgColor : normalBgColor);
		base.button.colors = colors;
	}

	public void Render(BaseArchiveData data)
	{
		if (data == null)
		{
			emptyTxt.text = base.staticTexts.GameDataTitle;
			base.grayed = true;
			return;
		}
		playerNameTxt.text = data.GetPlayerName();
		positionTxt.text = data.GetPositionTitle();
		gameDateTxt.text = data.GetGameDate();
		moneyTxt.text = data.GetMoney();
		timeSpanTxt.text = data.GetGameTimeSpan();
		realTimeTxt.text = data.realTimeStamp;
		base.grayed = false;
	}

	public void SetIndex(int index)
	{
		indexTxt.text = $"#{index + 1}";
	}
}
