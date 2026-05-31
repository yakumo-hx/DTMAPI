using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class CraftViewer<TData> : DolocUiObject where TData : ICraftData
{
	[SerializeField]
	protected Text title;

	[SerializeField]
	protected Image previewImg;

	[SerializeField]
	protected Text descInfo;

	[SerializeField]
	protected Text costInfo;

	[SerializeField]
	protected ConfirmCraftButton btnCraft;

	[SerializeField]
	protected Text emptyInfo;

	[SerializeField]
	protected CanvasGroup contentCanvas;

	[SerializeField]
	protected CostItemViewer costItemViewer;

	public ConfirmCraftButton BtnCraft => btnCraft;

	protected override void __Init()
	{
		base.__Init();
		costItemViewer.Init();
		btnCraft.Init();
		SetEmpty(value: true);
	}

	public void Show(TData data)
	{
		SetEmpty(!data.notEmpty);
		if (!data.notEmpty)
		{
			SetEmpty(value: true);
		}
		else
		{
			OnShow(data);
		}
	}

	protected virtual void OnShow(TData data)
	{
		title.text = data.recipeTitle;
		descInfo.text = data.description;
		SetSprite(previewImg, data.sceneSprite, autoSize: true);
		costItemViewer.Render(data.itemCosts);
		btnCraft.grayed = !data.isCostEnough;
		btnCraft.buttonText = data.buttonText;
		costInfo.text = data.itemCosts.costInfo;
	}

	public void Hide()
	{
	}

	public void SetEmpty(bool value)
	{
		contentCanvas.alpha = ((!value) ? 1 : 0);
		emptyInfo.text = GetEmptyInfo();
		emptyInfo.gameObject.SetActive(value);
	}

	protected abstract string GetEmptyInfo();

	public void RaiseCostItemsFadeUp()
	{
		costItemViewer.RaiseCostItemsFadeUp();
	}
}
