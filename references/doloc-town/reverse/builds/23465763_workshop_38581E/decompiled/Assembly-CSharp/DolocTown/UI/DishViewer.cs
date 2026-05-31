using DolocTown.Config.Recipe;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DishViewer : DolocUiObject
{
	[SerializeField]
	protected Text title;

	[SerializeField]
	protected Image previewImg;

	[SerializeField]
	protected Text descInfo;

	[SerializeField]
	protected Text commentInfo;

	[SerializeField]
	private Text outputCount;

	[SerializeField]
	private EffectGroupViewer effectGroupViewer;

	[SerializeField]
	private GameObject taskInfoObj;

	[SerializeField]
	private Text taskInfo;

	[SerializeField]
	private Text otherCount;

	protected override void __Init()
	{
		base.__Init();
		effectGroupViewer.Init();
	}

	public void SetTaskInfo(string text)
	{
		bool active = !text.IsNullOrEmpty();
		taskInfoObj.gameObject.SetActive(active);
		taskInfo.text = text;
	}

	public void RenderLocked(DishGroupInfo groupProto)
	{
		title.text = groupProto.UnknowTitle;
		descInfo.text = groupProto.UnknowDescription;
		SetSprite(previewImg, groupProto.UnknowIcon.Asset, autoSize: true);
		outputCount.text = "";
		commentInfo.text = base.staticTexts.RecipePanelUnknowDishComment;
		effectGroupViewer.SetVisible(value: false);
		otherCount.transform.parent.gameObject.SetActive(value: false);
	}

	public void Render(RecipeData data)
	{
		title.text = data.outputItemTitle;
		descInfo.text = data.description;
		SetSprite(previewImg, data.sceneSprite, autoSize: true);
		otherCount.transform.parent.gameObject.SetActive(value: true);
		otherCount.text = data.existItemCount.ToString();
		outputCount.text = data.outputCount;
		commentInfo.text = data.comment;
		if (data.itemEffects.notEmpty)
		{
			effectGroupViewer.SetVisible(value: true);
			effectGroupViewer.Render(data.itemEffects);
		}
		else
		{
			effectGroupViewer.SetVisible(value: false);
		}
	}

	public void SetCommentText(string text)
	{
		SetText(commentInfo, text);
	}

	protected string GetEmptyInfo()
	{
		return base.staticTexts.RecipePanelEmpty;
	}
}
