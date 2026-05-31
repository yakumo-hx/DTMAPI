using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemViewer : DolocUIPanel
{
	[SerializeField]
	private Image itemIcon;

	[SerializeField]
	private Text itemName;

	[SerializeField]
	private Text itemSource;

	[SerializeField]
	private Text itemType;

	[SerializeField]
	private Text itemDesc;

	[SerializeField]
	private Text itemPrice;

	[SerializeField]
	public ItemRecipeListViewer itemRecipeListViewer;

	[SerializeField]
	private CanvasGroup contentGroup;

	[SerializeField]
	private Text emptyHint;

	[SerializeField]
	private RectTransform itemInfoTransform;

	[SerializeField]
	private LayoutElement typeLayoutElement;

	[SerializeField]
	private float typePreferredWidth;

	[SerializeField]
	private GameObject recipeView;

	[SerializeField]
	private Text extraInfo;

	public ItemDetailData currentData { get; private set; }

	public void Render(ItemDetailData data)
	{
		currentData = data;
		contentGroup.alpha = (data.obtained ? 1 : 0);
		emptyHint.text = base.staticTexts.CollectionPanelItemUnknown;
		emptyHint.gameObject.SetActive(!data.obtained);
		recipeView.SetActive(value: true);
		itemRecipeListViewer.RefreshView(data.recipesData);
		extraInfo.gameObject.SetActive(value: false);
		if (data.obtained)
		{
			itemIcon.sprite = data.icon;
			itemName.text = data.name;
			itemType.text = data.type;
			itemSource.text = data.source;
			itemDesc.text = data.desc;
			itemPrice.text = data.price;
			int num = -6;
			float num2 = itemInfoTransform.rect.width;
			float preferredWidth = itemName.preferredWidth;
			float preferredWidth2 = itemType.preferredWidth;
			typeLayoutElement.preferredWidth = ((preferredWidth + preferredWidth2 <= num2) ? preferredWidth2 : Mathf.Max(num2 - preferredWidth, typePreferredWidth));
			if (preferredWidth + typeLayoutElement.preferredWidth > num2)
			{
				num = -48;
				typeLayoutElement.preferredWidth = Mathf.Max(num2 - itemSource.preferredWidth, typePreferredWidth);
			}
			itemType.rectTransform.anchoredPosition = new Vector2(0f, num);
			if (data.recipesData.Length == 0)
			{
				emptyHint.text = base.staticTexts.CollectionPanelItemRecipeEmpty;
				emptyHint.gameObject.SetActive(value: true);
			}
			if (!data.geneInfo.IsNullOrEmpty())
			{
				recipeView.SetActive(value: false);
				emptyHint.gameObject.SetActive(value: false);
				extraInfo.gameObject.SetActive(value: true);
				extraInfo.text = data.geneInfo;
			}
			itemRecipeListViewer.ResetNormalizedPosition();
		}
	}
}
