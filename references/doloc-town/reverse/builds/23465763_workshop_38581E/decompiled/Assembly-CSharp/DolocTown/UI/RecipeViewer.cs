using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class RecipeViewer : CraftViewer<RecipeData>
{
	[SerializeField]
	private Text maxCraftCountInfo;

	[SerializeField]
	private TextMeshProUGUI outputCount;

	[SerializeField]
	private Text extraInfo;

	private RectTransform extraInfoRect;

	[SerializeField]
	private float extraInfoMaxWidth = 300f;

	[SerializeField]
	private Text timeInfo;

	[SerializeField]
	private EffectGroupViewer effectGroupViewer;

	[SerializeField]
	private Text otherCount;

	[SerializeField]
	private GameObject taskInfoObj;

	[SerializeField]
	private Text taskInfo;

	protected override void __Init()
	{
		base.__Init();
		effectGroupViewer.Init();
		extraInfoRect = extraInfo.GetComponent<RectTransform>();
	}

	public void SetTaskInfo(string text)
	{
		bool flag = !text.IsNullOrEmpty();
		btnCraft.gameObject.SetActive(!flag);
		taskInfoObj.gameObject.SetActive(flag);
		taskInfo.text = text;
	}

	protected override void OnShow(RecipeData data)
	{
		base.OnShow(data);
		outputCount.text = data.outputCount;
		timeInfo.text = data.costTimeInfo;
		otherCount.text = data.existItemCount.ToString();
		otherCount.transform.parent.gameObject.SetActive(data.showExistItemCount);
		if (data.itemEffects.notEmpty)
		{
			effectGroupViewer.SetVisible(value: true);
			effectGroupViewer.Render(data.itemEffects);
		}
		else
		{
			effectGroupViewer.SetVisible(value: false);
		}
		SetText(maxCraftCountInfo, data.maxCraftCountInfo);
		SetText(extraInfo, data.detailInfo);
		if (!data.detailInfo.IsNullOrEmpty())
		{
			extraInfoRect.sizeDelta = new Vector2(Mathf.Min(extraInfo.preferredWidth, extraInfoMaxWidth), extraInfoRect.sizeDelta.y);
		}
	}

	protected override string GetEmptyInfo()
	{
		return base.staticTexts.RecipePanelEmpty;
	}
}
